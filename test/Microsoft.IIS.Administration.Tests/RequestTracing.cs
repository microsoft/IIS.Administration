// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Tests
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using Xunit;

    public class RequestTracing
    {
        public static readonly string REQUEST_TRACING_URL = $"{Configuration.Instance().TEST_SERVER_URL}/api/webserver/http-request-tracing";

        [Fact]
        public void CreatePatchRemoveRule()
        {
            var TEST_RULE_PATH = "test_rule*.path";
            var TEST_RULE_STATUS_CODES = new string[] { "101", "244-245", "280-301", "340"};
            var TEST_RULE_MIN_TIME = 100;
            var TEST_RULE_EVENT_SEVERITY = "error";
            var TEST_RULE_PROVIDER_NAME = "ASP";
            var TEST_RULE_ALLOWED_AREAS = new Dictionary<string,bool>() {
            };

            var PATCH_PATH = "test_patch*.path";
            var PATCH_RULE_STATUS_CODES = new string[] { "104-181", "333" };
            var PATCH_RULE_MIN_TIME = 103;
            var PATCH_RULE_EVENT_SEVERITY = "criticalerror";
            var PATCH_RULE_PROVIDER_NAME = "WWW Server";
            var PATCH_RULE_ALLOWED_AREAS = new Dictionary<string, bool>() {
                { "Security", true },
                { "Compression", true },
                { "Module", true }
            };


            using (var client = ApiHttpClient.Create()) {
                var feature = GetFeature(client, null, null);

                var rulesObj = Utils.FollowLink(client, feature, "rules");
                var rules = rulesObj.Value<JArray>("rules").ToObject<IEnumerable<JObject>>();

                var providersObj = Utils.FollowLink(client, feature, "providers");
                var providers = providersObj.Value<JArray>("providers").ToObject<IEnumerable<JObject>>();

                // Ensure rule with test path doesn't already exist
                foreach (var r in rules) {
                    if (r.Value<string>("path").Equals(TEST_RULE_PATH, StringComparison.OrdinalIgnoreCase)
                        || r.Value<string>("path").Equals(PATCH_PATH, StringComparison.OrdinalIgnoreCase)) {
                        Assert.True(client.Delete(Utils.Self(r)));
                        break;
                    }
                }
                
                var testRule = new {
                    path = TEST_RULE_PATH,
                    status_codes = TEST_RULE_STATUS_CODES,
                    min_request_execution_time = TEST_RULE_MIN_TIME,
                    event_severity = TEST_RULE_EVENT_SEVERITY,
                    traces = new [] {
                        new {
                            allowed_areas = TEST_RULE_ALLOWED_AREAS,
                            provider = providers.FirstOrDefault(p => p.Value<string>("name").Equals(TEST_RULE_PROVIDER_NAME)),
                            verbosity = "verbose"
                        }
                    },
                    request_tracing = feature
                };
                var patchRule = new {
                    path = PATCH_PATH,
                    status_codes = PATCH_RULE_STATUS_CODES,
                    min_request_execution_time = PATCH_RULE_MIN_TIME,
                    event_severity = PATCH_RULE_EVENT_SEVERITY,
                    traces = new[] {
                        new {
                            allowed_areas = PATCH_RULE_ALLOWED_AREAS,
                            provider = providers.FirstOrDefault(p => p.Value<string>("name").Equals(PATCH_RULE_PROVIDER_NAME)),
                            verbosity = "verbose"
                        }
                    },
                };



                var jRule = JObject.FromObject(testRule);
                var pRule = JObject.FromObject(patchRule);

                string result;
                Assert.True(client.Post(Utils.GetLink(feature, "rules"), JsonConvert.SerializeObject(testRule), out result));
                JObject newRule = null;

                try {
                    newRule = Utils.ToJ(result);

                    CompareRules(jRule, newRule);

                    Assert.True(client.Patch(Utils.Self(newRule), JsonConvert.SerializeObject(patchRule), out result));
                    newRule = Utils.ToJ(result);

                    CompareRules(pRule, newRule);

                }
                finally {
                    Assert.True(client.Delete(Utils.Self(newRule)));
                }
            }
        }

        [Fact]
        public void CreatePatchRemoveProvider()
        {
            var TEST_PROVIDER_NAME = "Test Provider";
            var TEST_PROVIDER_GUID = Guid.NewGuid().ToString("B");
            var TEST_AREAS = new string[] {
                "test_area",
                "test_area2"
            };
            var PATCH_PROVIDER_NAME = "Patch Provider";
            var PATCH_PROVIDER_GUID = Guid.NewGuid().ToString("B");
            var PATCH_AREAS = new string[] {
                "patch_area",
                "patch_area2"
            };


            using (var client = ApiHttpClient.Create()) {
                var feature = GetFeature(client, null, null);

                var providersObj = Utils.FollowLink(client, feature, "providers");
                var providers = providersObj.Value<JArray>("providers").ToObject<IEnumerable<JObject>>();
                
                foreach (var p in providers) {
                    if (p.Value<string>("name").Equals(TEST_PROVIDER_NAME, StringComparison.OrdinalIgnoreCase)
                        || p.Value<string>("name").Equals(PATCH_PROVIDER_NAME, StringComparison.OrdinalIgnoreCase)) {
                        Assert.True(client.Delete(Utils.Self(p)));
                        break;
                    }
                }

                var testProvider = new {
                    name = TEST_PROVIDER_NAME,
                    guid = TEST_PROVIDER_GUID,
                    areas = TEST_AREAS,
                    request_tracing = feature
                };
                var patchProvider = new {
                    name = PATCH_PROVIDER_NAME,
                    guid = PATCH_PROVIDER_GUID,
                    areas = PATCH_AREAS
                };

                var jProvider = JObject.FromObject(testProvider);
                var pProvider = JObject.FromObject(patchProvider);

                string result;
                Assert.True(client.Post(Utils.GetLink(feature, "providers"), JsonConvert.SerializeObject(testProvider), out result));
                JObject newProvider = null;

                try {
                    newProvider = Utils.ToJ(result);
                    CompareProviders(jProvider, newProvider);

                    Assert.True(client.Patch(Utils.Self(newProvider), JsonConvert.SerializeObject(patchProvider), out result));
                    newProvider = Utils.ToJ(result);

                    CompareProviders(pProvider, newProvider);
                }
                finally {
                    Assert.True(client.Delete(Utils.Self(newProvider)));
                }
            }
        }

        public static JObject GetFeature(HttpClient client, string siteName, string path)
        {
            if (path != null) {
                if (!path.StartsWith("/")) {
                    throw new ArgumentException("path");
                }
            }

            string content;
            if (!client.Get(REQUEST_TRACING_URL + "?scope=" + siteName + path, out content)) {
                return null;
            }

            return Utils.ToJ(content);
        }

        private static void CompareRules(JObject rule1, JObject rule2)
        {
            Assert.True(Utils.JEquals<string>(rule1, rule2, "path"));

            var statusCodes1 = rule1.Value<JArray>("status_codes").ToObject<string[]>();
            var statusCodes2 = rule1.Value<JArray>("status_codes").ToObject<string[]>();

            Assert.True(statusCodes1.Length == statusCodes2.Length);
            for (int i = 0; i < statusCodes1.Length; i++) {
                Assert.Equal(statusCodes1[i], statusCodes2[i]);
            }
            
            Assert.True(Utils.JEquals<long>(rule1, rule2, "min_request_execution_time"));
            Assert.True(Utils.JEquals<string>(rule1, rule2, "event_severity"));

            var traceArea1 = rule1.Value<JArray>("traces").ToObject<List<JObject>>()[0];
            var traceArea2 = rule2.Value<JArray>("traces").ToObject<List<JObject>>()[0];
            var provider1 = traceArea1.Value<JObject>("provider");
            var provider2 = traceArea2.Value<JObject>("provider");

            var allowedAreas1 = traceArea1["allowed_areas"].ToObject<Dictionary<string, bool>>();
            var allowedAreas2 = traceArea2["allowed_areas"].ToObject<Dictionary<string, bool>>();

            foreach (var key in allowedAreas1.Keys) {
                if (allowedAreas1[key]) {
                    Assert.True(allowedAreas2[key]);
                }
            }

            Assert.True(Utils.JEquals<string>(provider1, provider2, "name"));
            Assert.True(Utils.JEquals<string>(traceArea1, traceArea2, "verbosity"));
        }

        private static void CompareProviders(JObject provider1, JObject provider2)
        {
            Assert.True(Utils.JEquals<string>(provider1, provider2, "name"));
            Assert.True(Utils.JEquals<string>(provider1, provider2, "guid"));

            var area1 = provider1.Value<JArray>("areas").ToObject<List<string>>();
            var area2 = provider2.Value<JArray>("areas").ToObject<List<string>>();

            for (int i = 0; i < area1.Count(); i++) {
                Assert.Equal<string>(area1[i], area2[i]);
            }
        }
    }
}
