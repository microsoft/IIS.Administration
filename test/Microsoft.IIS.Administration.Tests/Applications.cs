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

    public class Applications
    {
        private const string TEST_APPLICATION_SITE_NAME = "test_application_site";
        private static readonly string TEST_APPLICATION_SITE = $"{{ \"bindings\": [ {{ \"ip_address\": \"*\", \"port\": \"50307\", \"is_https\": \"false\" }} ], \"physical_path\": \"c:\\\\sites\\\\test_site\" , \"name\": \"{TEST_APPLICATION_SITE_NAME}\" }}";
        private static string TEST_APPLICATION = "{ \"path\" : \"/test_application\", \"physical_path\" : \"c:\\\\sites\\\\test_site\\\\test_application\", \"website\":{ \"id\" : \"{site_id}\" } }";
        ITestOutputHelper _output;

        public static readonly string APPLICATION_URL = $"{Configuration.TEST_SERVER_URL}/api/webserver/webapps";


        public Applications(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void CreateAndCleanup()
        {
            bool pass = false;
            using (HttpClient client = ApiHttpClient.Create()) { 
                _output.WriteLine($"Running Application tests with application {TEST_APPLICATION}");

                JObject site;

                Sites.EnsureNoSite(client, TEST_APPLICATION_SITE_NAME);

                if (Sites.CreateSite(client, TEST_APPLICATION_SITE, out site)) {

                    // Set up the application json with the newly created site uuid
                    string testApplication = TEST_APPLICATION.Replace("{site_id}", site.Value<string>("id"));

                    JObject testApp;

                    if (CreateApplication(client, testApplication, out testApp)) {

                        string testAppUri = Utils.Self(testApp);

                        pass = TestApplicationExists(client, testAppUri);

                        Assert.True(DeleteApplication(client, testAppUri));

                    }

                    Assert.True(Sites.DeleteSite(client, $"{Sites.SITE_URL}/{site.Value<string>("id")}"));

                }
                Assert.True(pass);
            }
        }

        public static bool CreateApplication(HttpClient client, string application, out JObject result)
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

        public static JObject CreateApplication(HttpClient client, string path, string physicalPath, JObject parentSite)
        {
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
