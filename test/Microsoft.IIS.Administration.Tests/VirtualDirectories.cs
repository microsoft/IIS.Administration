// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Tests
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Text;
    using Xunit;
    using Xunit.Abstractions;

    public class VirtualDirectories
    {
        private const string TEST_SITE_NAME = "test_vdir_site";
        ITestOutputHelper _output;

        public static readonly string VDIR_URL = $"{Configuration.Instance().TEST_SERVER_URL}/api/webserver/virtual-directories";
        public static readonly string TEST_VDIR_PATH = "/test_vdir";
        public static readonly string TEST_VDIR_PHYSICAL_PATH = Path.Combine(Sites.TEST_SITE_PATH, "test_vdir");

        public VirtualDirectories(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void CreateAndCleanup()
        {
            bool pass = false;
            using (HttpClient client = ApiHttpClient.Create()) {

                Sites.EnsureNoSite(client, TEST_SITE_NAME);

                JObject site = Sites.CreateSite(_output, client, TEST_SITE_NAME, 50308, Sites.TEST_SITE_PATH);

                if (site != null) {

                    JObject testApp = Applications.CreateApplication(client, "/test_vdir_application", Applications.TEST_APPLICATION_PHYSICAL_PATH, site);

                    if (testApp != null) {

                        JObject vdir = CreateVdir(client, TEST_VDIR_PATH, TEST_VDIR_PHYSICAL_PATH, testApp, false);
                        if (vdir != null) {

                            string vdirUri = Utils.Self(vdir);

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

        private static bool CreateVdir(HttpClient client, string virtualDirectory, out JObject result)
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

        public static JObject CreateVdir(HttpClient client, string path, string physicalPath, JObject parent, bool forSite, bool createDirectoryIfNotExist = true)
        {
            if (createDirectoryIfNotExist && ! Directory.Exists(physicalPath)) {
                Directory.CreateDirectory(physicalPath);
            }

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
