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

    public class Applications
    {
        private const string TEST_APPLICATION_SITE_NAME = "test_application_site";
        ITestOutputHelper _output;

        public static readonly string APPLICATION_URL = $"{Configuration.Instance().TEST_SERVER_URL}/api/webserver/webapps";
        public static readonly string TEST_APPLICATION_PHYSICAL_PATH = Path.Combine(Sites.TEST_SITE_PATH, "test_application");


        public Applications(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void CreateAndCleanup()
        {
            bool pass = false;
            using (HttpClient client = ApiHttpClient.Create()) { 
                JObject site = null;

                Sites.EnsureNoSite(client, TEST_APPLICATION_SITE_NAME);
                site = Sites.CreateSite(_output, client, TEST_APPLICATION_SITE_NAME, 50307, Sites.TEST_SITE_PATH);

                if (site != null) {
                    JObject testApp = CreateApplication(client, "/test_application", TEST_APPLICATION_PHYSICAL_PATH, site);

                    if (testApp != null) {

                        string testAppUri = Utils.Self(testApp);

                        pass = TestApplicationExists(client, testAppUri);

                        Assert.True(DeleteApplication(client, testAppUri));

                    }

                    Assert.True(Sites.DeleteSite(client, $"{Sites.SITE_URL}/{site.Value<string>("id")}"));

                }
                Assert.True(pass);
            }
        }

        private static bool CreateApplication(HttpClient client, string application, out JObject result)
        {
            result = null;

            HttpContent content = new StringContent(application, Encoding.UTF8, "application/json");
            HttpResponseMessage response = client.PostAsync(APPLICATION_URL, content).Result;

            if (!Globals.Success(response)) {
                return false;
            }

            result = JsonConvert.DeserializeObject<JObject>(response.Content.ReadAsStringAsync().Result);

            return true;
        }

        public static JObject CreateApplication(HttpClient client, string path, string physicalPath, JObject parentSite, bool createDirectoryIfNotExist = true)
        {
            if (createDirectoryIfNotExist && !Directory.Exists(physicalPath)) {
                Directory.CreateDirectory(physicalPath);
            }

            var app = new {
                path = path,
                physical_path = physicalPath,
                website = new {
                    id = parentSite.Value<string>("id")
                }
            };

            string appStr = JsonConvert.SerializeObject(app);

            JObject result;
            if (!CreateApplication(client, appStr, out result)) {
                throw new Exception();
            }
            return result;
        }

        public static bool DeleteApplication(HttpClient client, string applicationUri)
        {
            if (!TestApplicationExists(client, applicationUri)) { throw new Exception("Can't delete application because it doesn't exist."); }
            HttpResponseMessage response = client.DeleteAsync(applicationUri).Result;
            return Globals.Success(response);
        }

        public static bool TestApplicationExists(HttpClient client, string applicationUri)
        {
            HttpResponseMessage responseMessage = client.GetAsync(applicationUri).Result;
            return Globals.Success(responseMessage);
        }
    }
}
