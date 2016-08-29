// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Tests
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using Xunit;

    public class Authorization
    {
        private const string TEST_SITE_NAME = "authorization_test_site";

        public static readonly string AUTHORIZATION_URL = $"{Globals.TEST_SERVER}:{Globals.TEST_PORT}/api/webserver/authorization";

        [Fact]
        public void ChangeAllProperties()
        {
            using (HttpClient client = ApiHttpClient.Create($"{Globals.TEST_SERVER}:{Globals.TEST_PORT}")) {

                Sites.EnsureNoSite(client, TEST_SITE_NAME);

                JObject site = Sites.CreateSite(client, TEST_SITE_NAME, 50310, @"c:\sites\test_site");
                JObject feature = GetAuthorizationFeature(client, site.Value<string>("name"), null);
                Assert.NotNull(feature);

                feature["bypass_login_pages"] = !feature.Value<bool>("bypass_login_pages");

                string result;
                string body = JsonConvert.SerializeObject(feature);

                Assert.True(client.Patch(Utils.Self(feature), body, out result));

                JObject newFeature = JsonConvert.DeserializeObject<JObject>(result);

                Assert.True(Utils.JEquals<bool>(feature, newFeature, "bypass_login_pages"));

                Sites.EnsureNoSite(client, TEST_SITE_NAME);
            }
        }

        [Fact]
        public void AddRemoveRule()
        {
            using (HttpClient client = ApiHttpClient.Create($"{Globals.TEST_SERVER}:{Globals.TEST_PORT}")) {

                Sites.EnsureNoSite(client, TEST_SITE_NAME);

                JObject site = Sites.CreateSite(client, TEST_SITE_NAME, 50310, @"c:\sites\test_site");
                JObject feature = GetAuthorizationFeature(client, site.Value<string>("name"), null);
                Assert.NotNull(feature);

                ClearRules(client, feature);

                var rulePayload = new {
                    users = "test_u",
                    roles = "test_r",
                    verbs = "test_v",
                    access_type = "deny",
                    authorization = feature
                };

                string result;
                Assert.True(client.Post(Utils.GetLink(feature, "rules"), JsonConvert.SerializeObject(rulePayload), out result));

                JObject rule = JsonConvert.DeserializeObject<JObject>(result);

                var conflictingRule = new {
                    users = "test_u",
                    roles = "test_r",
                    verbs = "test_v",
                    access_type = "allow",
                    authorization = feature
                };

                HttpContent content = new StringContent(JsonConvert.SerializeObject(conflictingRule), Encoding.UTF8, "application/json");

                var res = client.PostAsync(Utils.GetLink(feature, "rules"), content).Result;

                Assert.True(res.StatusCode == HttpStatusCode.Conflict);

                Assert.True(client.Delete(Utils.Self(rule)));

                Sites.EnsureNoSite(client, TEST_SITE_NAME);
            }
        }

        public static JObject GetAuthorizationFeature(HttpClient client, string siteName, string path)
        {
            if (path != null) {
                if (!path.StartsWith("/")) {
                    throw new ArgumentException("path");
                }
            }

            string content;
            if (!client.Get(AUTHORIZATION_URL + "?scope=" + siteName + path, out content)) {
                return null;
            }

            return Utils.ToJ(content);
        }

        public static void ClearRules(HttpClient client, JObject feature)
        {
            string result;
            Assert.True(client.Get(Utils.GetLink(feature, "rules"), out result));

            JArray rules = JsonConvert.DeserializeObject<JObject>(result).Value<JArray>("rules");

            foreach(JObject r in rules) {

                Assert.True(client.Delete(Utils.Self(r)));
            }
        }
    }
}
