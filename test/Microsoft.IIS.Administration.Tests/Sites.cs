// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Tests
{
    using WebServer;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using Xunit;
    using Xunit.Abstractions;
    using Core.Utils;
    using System.Net;
    using System.IO;
    using System.Threading;
    using System.Dynamic;

    public class Sites
    {
        private const string OIDServerAuth = "1.3.6.1.5.5.7.3.1";
        private const string TEST_SITE_NAME = "test_site";
        private const int TEST_PORT = 50306;
        private ITestOutputHelper _output;

        public static readonly string TEST_SITE_PATH = Path.Combine(Configuration.Instance().TEST_ROOT_PATH, TEST_SITE_NAME);
        public static readonly string SITE_URL = $"{Configuration.Instance().TEST_SERVER_URL}/api/webserver/websites";

        public static readonly string CertificatesUrl = $"{Configuration.Instance().TEST_SERVER_URL}/api/certificates";

        public Sites(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void CreateAndCleanup()
        {
            using (HttpClient client = ApiHttpClient.Create()) {

                EnsureNoSite(client, TEST_SITE_NAME);
                
                JObject site = CreateSite(_output, client, TEST_SITE_NAME, TEST_PORT, TEST_SITE_PATH);
                Assert.NotNull(site);

                _output.WriteLine("Create Site success.");

                string testSiteUri = $"{SITE_URL}/{site.Value<string>("id")}";

                Assert.True(SiteExists(client, testSiteUri));
                _output.WriteLine("Site Exists success.");

                Assert.True(DeleteSite(client, testSiteUri));
                _output.WriteLine("Delete Site success.");
            }
        }

        [Fact]
        public void ChangeAllProperties()
        {
            using (HttpClient client = ApiHttpClient.Create()) {

                EnsureNoSite(client, TEST_SITE_NAME);
                JObject site = CreateSite(_output, client, TEST_SITE_NAME, TEST_PORT, Configuration.Instance().TEST_ROOT_PATH);
                JObject cachedSite = new JObject(site);

                WaitForStatus(client, ref site);

                Assert.True(site != null);

                site["server_auto_start"] = !site.Value<bool>("server_auto_start");
                site["physical_path"] = Configuration.Instance().TEST_ROOT_PATH;
                site["enabled_protocols"] = site.Value<string>("enabled_protocols").Equals("http", StringComparison.OrdinalIgnoreCase) ? "https" : "http";

                // If site status is unknown then we don't know if it will be started or stopped when it becomes available
                // Utilizing the defaults we assume it will go from unkown to started
                site["status"] = Enum.GetName(typeof(Status),
                                                             DynamicHelper.To<Status>(site["status"]) ==
                                                             Status.Stopped ? Status.Started :
                                                             Status.Stopped);

                JObject limits = (JObject)site["limits"];
                limits["connection_timeout"] = limits.Value<long>("connection_timeout") - 1;
                limits["max_bandwidth"] = limits.Value<long>("max_bandwidth") - 1;
                limits["max_connections"] = limits.Value<long>("max_connections") - 1;
                limits["max_url_segments"] = limits.Value<long>("max_url_segments") - 1;
                
                JArray bindings = site.Value<JArray>("bindings");
                bindings.Clear();
                bindings.Add(JObject.FromObject(new {
                    port = 63014,
                    ip_address = "40.3.5.15",
                    hostname = "testhostname",
                    protocol = "http"
                }));
                bindings.Add(JObject.FromObject(new {
                    port = 63015,
                    ip_address = "*",
                    hostname = "",
                    protocol = "https",
                    certificate = GetCertificate(client)
                }));

                string body = JsonConvert.SerializeObject(site);
                var result = client.AssertPatch(Utils.Self(site), body);
                JObject newSite = JsonConvert.DeserializeObject<JObject>(result);
                WaitForStatus(client, ref newSite);

                Assert.True(Utils.JEquals<bool>(site, newSite, "server_auto_start"));
                Assert.True(Utils.JEquals<string>(site, newSite, "physical_path"));
                Assert.True(Utils.JEquals<string>(site, newSite, "enabled_protocols"));
                Assert.True(Utils.JEquals<string>(site, newSite, "status", StringComparison.OrdinalIgnoreCase));

                Assert.True(Utils.JEquals<long>(site, newSite, "limits.connection_timeout"));
                Assert.True(Utils.JEquals<long>(site, newSite, "limits.max_bandwidth"));
                Assert.True(Utils.JEquals<long>(site, newSite, "limits.max_connections"));
                Assert.True(Utils.JEquals<long>(site, newSite, "limits.max_url_segments"));

                for (var i = 0; i < bindings.Count; i++) {
                    var oldBinding = (JObject)bindings[i];
                    var newBinding = (JObject)bindings[i];

                    Assert.True(Utils.JEquals<string>(oldBinding, newBinding, "protocol"));
                    Assert.True(Utils.JEquals<string>(oldBinding, newBinding, "port"));
                    Assert.True(Utils.JEquals<string>(oldBinding, newBinding, "ip_address"));
                    Assert.True(Utils.JEquals<string>(oldBinding, newBinding, "hostname"));

                    if (newBinding.Value<string>("protocol").Equals("https")) {
                        Assert.True(JToken.DeepEquals(oldBinding["certificate"], newBinding["certificate"]));
                    }
                }

                    Assert.True(DeleteSite(client, Utils.Self(site)));
            }
        }
        [Fact]
        public void BindingConflict()
        {
            string[] httpProperties = new string[] { "ip_address", "port", "hostname", "protocol", "binding_information" };
            string[] httpsProperties = new string[] { "ip_address", "port", "hostname", "protocol", "binding_information", "certificate" };
            string[] othersProperties = new string[] { "protocol", "binding_information" };


            using (HttpClient client = ApiHttpClient.Create()) {
                EnsureNoSite(client, TEST_SITE_NAME);
                JObject site = CreateSite(_output, client, TEST_SITE_NAME, TEST_PORT, TEST_SITE_PATH);

                var bindings = site.Value<JArray>("bindings");
                bindings.Clear();

                var conflictBindings = new object[] {
                    new {
                        port = 63015,
                        ip_address = "*",
                        hostname = "abc",
                        protocol = "http"
                    },
                    new {
                        port = 63015,
                        ip_address = "*",
                        hostname = "abc",
                        protocol = "http"
                    }
                };

                foreach (var b in conflictBindings) {
                    bindings.Add(JObject.FromObject(b));
                }

                var response = client.PatchRaw(Utils.Self(site), site);
                Assert.True(response.StatusCode == HttpStatusCode.Conflict);

                conflictBindings = new object[] {
                    new {
                        binding_information = "35808:*",
                        protocol = "net.tcp"
                    },
                    new {
                        binding_information = "35808:*",
                        protocol = "net.tcp"
                    }
                };

                bindings.Clear();
                foreach (var b in conflictBindings) {
                    bindings.Add(JObject.FromObject(b));
                }

                response = client.PatchRaw(Utils.Self(site), site);
                Assert.True(response.StatusCode == HttpStatusCode.Conflict);


                Assert.True(DeleteSite(client, Utils.Self(site)));
            }
        }

        [Fact]
        public void BindingTypes()
        {
            IEnumerable<string> httpProperties = new string[] { "ip_address", "port", "hostname", "protocol", "binding_information" };
            List<string> httpsProperties = new List<string> { "ip_address", "port", "hostname", "protocol", "binding_information", "certificate" };
            IEnumerable<string> othersProperties = new string[] { "protocol", "binding_information" };

            if (Utils.OsVersion >= new Version(6, 2)) {
                httpsProperties.Add("require_sni");
            }

            using (HttpClient client = ApiHttpClient.Create()) {

                EnsureNoSite(client, TEST_SITE_NAME);
                JObject site = CreateSite(_output, client, TEST_SITE_NAME, TEST_PORT, TEST_SITE_PATH);

                var bindings = site.Value<JArray>("bindings");
                bindings.Clear();
                int p = 63013;

                var goodBindings = new object[] {
                    new {
                        port = p++,
                        ip_address = "*",
                        hostname = "abc",
                        protocol = "http"
                    },
                    new {
                        binding_information = "128.0.3.5:" + (p++) + ":def",
                        protocol = "http"
                    },
                    new {
                        port = p++,
                        ip_address = "*",
                        hostname = "",
                        protocol = "https",
                        certificate = GetCertificate(client)
                    },
                    new {
                        binding_information = "*:" + (p++) + ":def",
                        protocol = "http"
                    },
                    new {
                        binding_information = (p++) + ":*",
                        protocol = "net.tcp"
                    },
                    new {
                        binding_information = "*",
                        protocol = "net.pipe"
                    }
                };

                foreach (var b in goodBindings) {
                    bindings.Add(JObject.FromObject(b));
                }

                var res = client.Patch(Utils.Self(site), site);
                Assert.NotNull(res);

                JArray newBindings = res.Value<JArray>("bindings");
                Assert.True(bindings.Count == newBindings.Count);

                for (var i = 0; i < bindings.Count; i++) {
                    var binding = (JObject)bindings[i];
                    foreach (var prop in binding.Properties()) {
                        Assert.True(JToken.DeepEquals(binding[prop.Name], newBindings[i][prop.Name]));
                    }

                    string protocol = binding.Value<string>("protocol");

                    switch (protocol) {
                        case "http":
                            Assert.True(HasExactProperties((JObject)newBindings[i], httpProperties));
                            break;
                        case "https":
                            Assert.True(HasExactProperties((JObject)newBindings[i], httpsProperties));
                            break;
                        default:
                            Assert.True(HasExactProperties((JObject)newBindings[i], othersProperties));
                            break;
                    }
                }

                var badBindings = new object[] {
                    new {
                        port = p++,
                        ip_address = "*",
                        hostname = "abc"
                    },
                    new {
                        port = p++,
                        ip_address = "",
                        hostname = "abc",
                        protocol = "http"
                    },
                    new {
                        protocol = "http",
                        binding_information = $":{p++}:"
                    },
                    new {
                        protocol = "http",
                        binding_information = $"127.0.4.3::"
                    }
                };

                foreach (var badBinding in badBindings) {
                    newBindings.Clear();
                    newBindings.Add(JObject.FromObject(badBinding));
                    var response = client.PatchRaw(Utils.Self(res), res);
                    Assert.True(response.StatusCode == HttpStatusCode.BadRequest);
                }

                Assert.True(DeleteSite(client, Utils.Self(site)));
            }
        }

        [Fact]
        public void Sni()
        {
            if (Utils.OsVersion < new Version(6, 2)) {
                return;
            }

            using (HttpClient client = ApiHttpClient.Create()) {

                EnsureNoSite(client, TEST_SITE_NAME);
                JObject site = CreateSite(_output, client, TEST_SITE_NAME, TEST_PORT, TEST_SITE_PATH);

                var bindings = site.Value<JArray>("bindings");
                bindings.Clear();
                int p = 63013;

                var goodBindings = new object[] {
                    new {
                        port = p++,
                        ip_address = "*",
                        hostname = "test_host_name",
                        protocol = "https",
                        certificate = GetCertificate(client),
                        require_sni = true
                    }
                };

                foreach (var b in goodBindings) {
                    bindings.Add(JObject.FromObject(b));
                }

                var res = client.Patch(Utils.Self(site), site);
                Assert.NotNull(res);

                JArray newBindings = res.Value<JArray>("bindings");
                Assert.True(bindings.Count == newBindings.Count);

                for (var i = 0; i < bindings.Count; i++) {
                    var binding = (JObject)bindings[i];
                    foreach (var prop in binding.Properties()) {
                        Assert.True(JToken.DeepEquals(binding[prop.Name], newBindings[i][prop.Name]));
                    }

                    string protocol = binding.Value<string>("protocol");
                }

                var badBindings = new object[] {
                    new {
                        port = p++,
                        ip_address = "*",
                        hostname = "",
                        protocol = "https",
                        certificate = GetCertificate(client),
                        require_sni = true
                    }
                };

                foreach (var badBinding in badBindings) {
                    newBindings.Clear();
                    newBindings.Add(JObject.FromObject(badBinding));
                    var response = client.PatchRaw(Utils.Self(res), res);
                    Assert.True(response.StatusCode == HttpStatusCode.BadRequest);
                }

                Assert.True(DeleteSite(client, Utils.Self(site)));
            }
        }

        [Theory]
        [InlineData(99722)]
        public void CreateWithKey(int key)
        {
            using (HttpClient client = ApiHttpClient.Create()) {

                EnsureNoSite(client, TEST_SITE_NAME);

                var siteData = new {
                    name = TEST_SITE_NAME,
                    physical_path = TEST_SITE_PATH,
                    key = key,
                    bindings = new object[] {
                        new {
                            ip_address = "*",
                            port = TEST_PORT,
                            protocol = "http"
                        }
                    }
                };

                JObject site = client.Post(SITE_URL, siteData);
                Assert.NotNull(site);

                Assert.Equal(key, site.Value<int>("key"));

                Assert.True(client.Delete(Utils.Self(site)));
            }
        }

        private bool HasExactProperties(JObject obj, IEnumerable<string> names) {
            if (obj.Properties().Count() != names.Count()) {
                return false;
            }

            foreach (var property in obj.Properties()) {
                if (!names.Contains(property.Name)) {
                    return false;
                }
            }
            return true;
        }

        [Theory]
        [InlineData(10)]
        public void GetSites(int n)
        {
            using (HttpClient client = ApiHttpClient.Create()) {

                string result;
                for(int i = 0; i < n; i++) {
                    
                    Assert.True(client.Get(SITE_URL, out result));
                }
            }
        }

        private static bool CreateSite(ITestOutputHelper output, HttpClient client, string testSite, out JObject site, bool createDirectoryIfNotExist = true)
        {
            site = null;
            HttpContent content = new StringContent(testSite, Encoding.UTF8, "application/json");
            HttpResponseMessage response = client.PostAsync(SITE_URL, content).Result;

            if (!Globals.Success(response))
            {
                output.WriteLine("Non-Success response:");
                output.WriteLine(response.Content.ReadAsStringAsync().Result);
                return false;
            }

            site = JsonConvert.DeserializeObject<JObject>(response.Content.ReadAsStringAsync().Result);

            return true;
        }

        public static JObject CreateSite(ITestOutputHelper output, HttpClient client, string name, int port, string physicalPath, bool createDirectoryIfNotExist = true, JObject appPool = null)
        {
            if (createDirectoryIfNotExist && !Directory.Exists(physicalPath)) {
                Directory.CreateDirectory(physicalPath);

                File.WriteAllText(Path.Combine(physicalPath, "iisstart.htm"), "Default file");
            }

            string iisstart = Path.Combine(physicalPath, "iisstart.htm");
            if (createDirectoryIfNotExist && !File.Exists(iisstart)) {
                Directory.CreateDirectory(physicalPath);

                File.WriteAllText(iisstart, "Default file");
            }

            dynamic site = new ExpandoObject();
            site.name = name;
            site.physical_path = physicalPath;
            site.bindings = new object[] {
                new {
                    ip_address = "*",
                    port = port.ToString(),
                    protocol = "http"
                }
            };

            if (appPool != null) {
                site.application_pool = new {
                    id = appPool.Value<string>("id")
                };
            }

            string siteStr = JsonConvert.SerializeObject(site);

            JObject result;

            if(!CreateSite(output, client, siteStr, out result)) {
                throw new Exception();
            }

            return result;
        }

        public static bool DeleteSite(HttpClient client, string siteUri)
        {
            if (!SiteExists(client, siteUri)) { throw new Exception("Can't delete test site because it doesn't exist."); }
            HttpResponseMessage response = client.DeleteAsync(siteUri).Result;
            return Globals.Success(response);
        }

        public static JObject GetSite(HttpClient client, string name)
        {
            var site = client.Get(SITE_URL)["websites"]
                        .ToObject<IEnumerable<JObject>>()
                        .FirstOrDefault(p => p.Value<string>("name").Equals(name, StringComparison.OrdinalIgnoreCase));

            return site == null ? null : Utils.FollowLink(client, site, "self");
        }

        public static void EnsureNoSite(HttpClient client, string siteName)
        {
            JObject site = GetSite(client, siteName);

            if (site == null) {
                return;
            }

            if(!DeleteSite(client, Utils.Self(site))) {
                throw new Exception();
            }
        }

        private static bool SiteExists(HttpClient client, string siteUri)
        {
            HttpResponseMessage responseMessage = client.GetAsync(siteUri).Result;
            return Globals.Success(responseMessage);
        }

        private void WaitForStatus(HttpClient client, ref JObject site)
        {
            string res;
            int refreshCount = 0;
            while (site.Value<string>("status") == "unknown") {
                refreshCount++;
                if (refreshCount > 500) {
                    throw new Exception();
                }

                Thread.Sleep(10);
                client.Get(Utils.Self(site), out res);
                site = JsonConvert.DeserializeObject<JObject>(res);
            }
        }

        private JObject GetCertificate(HttpClient client)
        {
            string result = client.AssertGet(CertificatesUrl + $"?intended_purpose={OIDServerAuth}");
            var certsObj = JObject.Parse(result);
            var allCerts = certsObj.Value<JArray>("certificates");
            Assert.NotEmpty(allCerts);
            var localCerts = allCerts.Where(c => c["subject"].Value<string>() == "CN=localhost");
            Assert.NotEmpty(localCerts);
            var defaultCertName = "Microsoft IIS Administration Server Certificate";
            var cert = localCerts.FirstOrDefault(c => c["alias"].Value<string>() == defaultCertName);
            if (cert == null)
            {
                cert = localCerts.First();
                _output.WriteLine($"[WARNING]: unable to find {defaultCertName}, using {cert["alias"].Value<string>()} for tests instead.");
            }
            return cert.ToObject<JObject>();
        }
    }
}
