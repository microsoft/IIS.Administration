// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Tests
{
    using Microsoft.IIS.Administration.WebServer;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using Xunit;
    using Xunit.Abstractions;
    using Microsoft.IIS.Administration.Core.Utils;

    public class Sites
    {
        private const string TEST_SITE_NAME = "test_site";
        private static readonly string TEST_SITE = $"{{ \"bindings\": [ {{ \"ip_address\": \"*\", \"port\": \"50306\", \"is_https\": \"false\" }} ], \"physical_path\": \"c:\\\\sites\\\\test_site\" , \"name\": \"{TEST_SITE_NAME}\" }}";
        private ITestOutputHelper _output;

        public static readonly string SITE_URL = $"{Globals.TEST_SERVER}:{Globals.TEST_PORT}/api/webserver/websites";

        public Sites(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void CreateAndCleanup()
        {
            using (HttpClient client = ApiHttpClient.Create($"{Globals.TEST_SERVER}:{Globals.TEST_PORT}")) {

                _output.WriteLine($"Running tests with site: {TEST_SITE}");

                EnsureNoSite(client, TEST_SITE_NAME);

                JObject site;

                Assert.True(CreateSite(client, TEST_SITE, out site));
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
            using (HttpClient client = ApiHttpClient.Create($"{Globals.TEST_SERVER}:{Globals.TEST_PORT}")) {

                EnsureNoSite(client, TEST_SITE_NAME);
                JObject site = CreateSite(client, TEST_SITE_NAME, 50306, @"c:\sites\test_site");
                JObject cachedSite = new JObject(site);

                WaitForStatus(client, site);

                Assert.True(site != null);

                site["server_auto_start"] = !site.Value<bool>("server_auto_start");
                site["physical_path"] = @"c:\\sites";
                site["enabled_protocols"] = "bogus_protocol";

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
                JObject binding = (JObject)bindings.First;
                binding["port"] = 63014;
                binding["ip_address"] = "40.3.5.15";
                binding["hostname"] = "testhostname";

                string result;
                string body = JsonConvert.SerializeObject(site);

                Assert.True(client.Patch(Utils.Self(site), body, out result));

                JObject newSite = JsonConvert.DeserializeObject<JObject>(result);

                Assert.True(Utils.JEquals<bool>(site, newSite, "server_auto_start"));
                Assert.True(Utils.JEquals<string>(site, newSite, "physical_path"));
                Assert.True(Utils.JEquals<string>(site, newSite, "enabled_protocols"));
                Assert.True(Utils.JEquals<string>(site, newSite, "status", StringComparison.OrdinalIgnoreCase));

                Assert.True(Utils.JEquals<long>(site, newSite, "limits.connection_timeout"));
                Assert.True(Utils.JEquals<long>(site, newSite, "limits.max_bandwidth"));
                Assert.True(Utils.JEquals<long>(site, newSite, "limits.max_connections"));
                Assert.True(Utils.JEquals<long>(site, newSite, "limits.max_url_segments"));

                JObject oldBinding = (JObject)site.Value<JArray>("bindings").First;
                JObject newBinding = (JObject)newSite.Value<JArray>("bindings").First;

                Assert.True(Utils.JEquals<string>(oldBinding, newBinding, "port"));
                Assert.True(Utils.JEquals<string>(oldBinding, newBinding, "ip_address"));
                Assert.True(Utils.JEquals<string>(oldBinding, newBinding, "hostname"));


                Assert.True(DeleteSite(client, Utils.Self(site)));
            }
        }

        [Theory]
        [InlineData(10)]
        public void GetSites(int n)
        {
            using (HttpClient client = ApiHttpClient.Create($"{Globals.TEST_SERVER}:{Globals.TEST_PORT}")) {

                string result;
                for(int i = 0; i < n; i++) {
                    
                    Assert.True(client.Get(SITE_URL, out result));
                }
            }
        }

        public static bool CreateSite(HttpClient client, string testSite, out JObject site)
        {
            site = null;
            HttpContent content = new StringContent(testSite, Encoding.UTF8, "application/json");
            HttpResponseMessage response = client.PostAsync(SITE_URL, content).Result;

            if (!Globals.Success(response)) {
                return false;
            }

            site = JsonConvert.DeserializeObject<JObject>(response.Content.ReadAsStringAsync().Result);

            return true;
        }

        public static JObject CreateSite(HttpClient client, string name, int port, string physicalPath)
        {

            var site = new {
                name = name,
                physical_path = physicalPath,
                bindings = new object[] {
                    new {
                        ip_address = "*",
                        port = port.ToString(),
                        is_https = false
                    }
                }
            };

            string siteStr = JsonConvert.SerializeObject(site);

            JObject result;
            if(!CreateSite(client, siteStr, out result)) {
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

        public static bool GetSites(HttpClient client, out List<JObject> sites)
        {
            string response = null;
            sites = null;

            if(!client.Get(SITE_URL, out response)) {
                return false;
            }

            JObject jObj = JsonConvert.DeserializeObject<JObject>(response);

            JArray sArr = jObj["websites"] as JArray;
            sites = new List<JObject>();

            foreach(JObject site in sArr) {
                sites.Add(site);
            }

            return true;
        }

        public static JObject GetSite(HttpClient client, string siteName)
        {
            List<JObject> sites;

            if (!(GetSites(client, out sites))) {
                return null;
            }

            JObject siteRef =  sites.FirstOrDefault(s => {
                string name = DynamicHelper.Value(s["name"]);

                return name == null ? false : name.Equals(siteName, StringComparison.OrdinalIgnoreCase);
            });

            if(siteRef == null) {
                return null;
            }

            string siteContent;
            if(client.Get($"{ Globals.TEST_SERVER }:{ Globals.TEST_PORT }{ siteRef["_links"]["self"].Value<string>("href") }", out siteContent)) {

                return JsonConvert.DeserializeObject<JObject>(siteContent);
            }

            return null;
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

        private void WaitForStatus(HttpClient client, JObject site)
        {
            string res;
            int refreshCount = 0;
            while (site.Value<string>("status") == "unknown") {
                refreshCount++;
                if (refreshCount > 100) {
                    throw new Exception();
                }

                client.Get(Utils.Self(site), out res);
                site = JsonConvert.DeserializeObject<JObject>(res);
            }
        }
    }
}
