// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Tests
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Net.Http;
    using System.Text;
    using Xunit;
    using Xunit.Abstractions;

    public class VirtualDirectories
    {
        private const string TEST_SITE_NAME = "test_vdir_site";
        public static readonly string TEST_SITE = $"{{ \"bindings\": [ {{ \"ip_address\": \"*\", \"port\": \"50308\", \"is_https\": \"false\" }} ], \"physical_path\": \"c:\\\\sites\\\\test_site\" , \"name\": \"{TEST_SITE_NAME}\" }}";
        public static string TEST_APPLICATION = "{ \"path\" : \"/test_vdir_application\", \"physical_path\" : \"c:\\\\sites\\\\test_site\\\\test_application\", \"website\" : { \"id\" : \"{site_id}\" } }";
        public static string TEST_VDIR = "{ \"path\" : \"/test_vdir\", \"physical_path\" : \"c:\\\\sites\\\\test_site\\\\test_vdir\", \"webapp\" : { \"id\" : \"{app_id}\" } }";
        ITestOutputHelper _output;

        public static readonly string VDIR_URL = $"{Configuration.TEST_SERVER_URL}/api/webserver/virtual-directories";

        public VirtualDirectories(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void CreateAndCleanup()
        {
            bool pass = false;
            using (HttpClient client = ApiHttpClient.Create()) {
                _output.WriteLine($"Running Virtual Directory tests with Virtual Directory {TEST_VDIR}");

                Sites.EnsureNoSite(client, TEST_SITE_NAME);

                JObject site;

                if (Sites.CreateSite(client, TEST_SITE, out site)) {

                    string app = TEST_APPLICATION.Replace("{site_id}", site.Value<string>("id"));

                    JObject testApp;

                    if (Applications.CreateApplication(client, app, out testApp)) {

                        string vdir = TEST_VDIR.Replace("{app_id}", testApp.Value<string>("id"));

                        JObject jVdir;
                        if (CreateVdir(client, vdir, out jVdir)) {

                            string vdirUri = Utils.Self(jVdir);

                            pass = VdirExists(client, vdirUri);

                            Assert.True(DeleteVdir(client, vdirUri));

                        }

                        Assert.True(Applications.DeleteApplication(client, Utils.Self(testApp)));

                    }

                    Assert.True(Sites.DeleteSite(client, Utils.Self(site)));
                }
                Assert.True(pass);
            }
        }

        public static bool CreateVdir(HttpClient client, string virtualDirectory, out JObject result)
        {
            result = null;

            HttpContent content = new StringContent(virtualDirectory, Encoding.UTF8, "application/json");
            HttpResponseMessage response = client.PostAsync(VDIR_URL, content).Result;
            if (!Globals.Success(response)) {
                return false;
            }

            result = JsonConvert.DeserializeObject<JObject>(response.Content.ReadAsStringAsync().Result);

            return true;
        }

        public static JObject CreateVdir(HttpClient client, string path, string physicalPath, JObject parent, bool forSite = true)
        {
            object vdir = null;
            if(forSite) {
                vdir = new {
                    path = path,
                    physical_path = physicalPath,
                    website = new {
                        id = parent.Value<string>("id")
                    }
                };
            }
            else {
                vdir = new {
                    path = path,
                    physical_path = physicalPath,
                    webapp = new {
                        id = parent.Value<string>("id")
                    }
                };
            }

            string vdirStr = JsonConvert.SerializeObject(vdir);

            JObject result;
            if (!CreateVdir(client, vdirStr, out result)) {
                throw new Exception();
            }
            return result;
        }

        public static bool DeleteVdir(HttpClient client, string vdirUri)
        {
            if (!VdirExists(client, vdirUri)) { throw new Exception("Can't delete test application because it doesn't exist."); }
            HttpResponseMessage response = client.DeleteAsync(vdirUri).Result;
            return Globals.Success(response);
        }

        public static bool VdirExists(HttpClient client, string vdirUri)
        {
            HttpResponseMessage responseMessage = client.GetAsync(vdirUri).Result;
            return Globals.Success(responseMessage);
        }
    }
}
