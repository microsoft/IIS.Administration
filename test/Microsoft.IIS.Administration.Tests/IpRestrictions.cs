// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Tests
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Net.Http;
    using Xunit;
    public class IpRestrictions
    {
        public static readonly string IP_RESTRICTIONS_URL = $"{Configuration.Instance().TEST_SERVER_URL}/api/webserver/ip-restrictions";

        [Fact]
        public void ChangeAllProperties()
        {
            using (HttpClient client = ApiHttpClient.Create()) {

                JObject ipRes = GetIpRestrictionsFeature(client, null, null);
                ipRes.Remove("enabled");
                JObject cachedFeature = new JObject(ipRes);

                ipRes["allow_unlisted"] = !ipRes.Value<bool>("allow_unlisted");
                ipRes["enable_reverse_dns"] = !ipRes.Value<bool>("enable_reverse_dns");
                ipRes["enable_proxy_mode"] = !ipRes.Value<bool>("enable_proxy_mode");
                ipRes["logging_only_mode"] = !ipRes.Value<bool>("logging_only_mode");
                ipRes["deny_action"] = "NotFound";

                JObject denyByConcurrentRequests = ipRes.Value<JObject>("deny_by_concurrent_requests");
                denyByConcurrentRequests["enabled"] = !ipRes.Value<bool>("enabled");
                denyByConcurrentRequests["max_concurrent_requests"] = denyByConcurrentRequests.Value<long>("max_concurrent_requests") + 1;

                JObject denyByRequestRate = ipRes.Value<JObject>("deny_by_request_rate");
                denyByRequestRate["enabled"] = !denyByRequestRate.Value<bool>("enabled");
                denyByRequestRate["max_requests"] = denyByRequestRate.Value<long>("max_requests") + 1;
                denyByRequestRate["time_period"] = denyByRequestRate.Value<long>("time_period") + 1;

                string result;
                string body = JsonConvert.SerializeObject(ipRes);

                Assert.True(client.Patch(Utils.Self(ipRes), body, out result));

                JObject newIpRes = JsonConvert.DeserializeObject<JObject>(result);

                Assert.True(Utils.JEquals<bool>(ipRes, newIpRes, "allow_unlisted"));
                Assert.True(Utils.JEquals<bool>(ipRes, newIpRes, "enable_reverse_dns"));
                Assert.True(Utils.JEquals<bool>(ipRes, newIpRes, "enable_proxy_mode"));
                Assert.True(Utils.JEquals<bool>(ipRes, newIpRes, "logging_only_mode"));
                Assert.True(Utils.JEquals<string>(ipRes, newIpRes, "deny_action", StringComparison.OrdinalIgnoreCase));

                Assert.True(Utils.JEquals<bool>(ipRes, newIpRes, "deny_by_concurrent_requests.enabled"));
                Assert.True(Utils.JEquals<long>(ipRes, newIpRes, "deny_by_concurrent_requests.max_concurrent_requests"));

                Assert.True(Utils.JEquals<bool>(ipRes, newIpRes, "deny_by_request_rate.enabled"));
                Assert.True(Utils.JEquals<long>(ipRes, newIpRes, "deny_by_request_rate.max_requests"));
                Assert.True(Utils.JEquals<long>(ipRes, newIpRes, "deny_by_request_rate.time_period"));

                // Create json payload of original feature state
                body = JsonConvert.SerializeObject(cachedFeature);

                // Patch request filtering to original state
                Assert.True(client.Patch(Utils.Self(newIpRes), body, out result));
            }
        }

        [Fact]
        public void CreateRemoveRule()
        {
            using (HttpClient client = ApiHttpClient.Create()) {


                JObject feature = GetIpRestrictionsFeature(client, null, null);

                string rulesLink = Utils.GetLink(feature, "entries");

                var rule = new {
                    allowed = false,
                    ip_address = "127.255.255.254",
                    ip_restriction = feature
                };

                string result;
                Assert.True(client.Get(rulesLink, out result));

                JObject rulesRep = JsonConvert.DeserializeObject<JObject>(result);
                JArray rules = rulesRep.Value<JArray>("entries");

                foreach(JObject r in rules)
                {
                    if(r.Value<string>("ip_address").Equals("127.255.255.254"))
                    {
                        Assert.True(client.Delete(Utils.Self(r)));
                    }
                }

                Assert.True(client.Post(rulesLink, JsonConvert.SerializeObject(rule), out result));

                JObject newObject = JsonConvert.DeserializeObject<JObject>(result);

                Assert.True(client.Delete(Utils.Self(newObject)));
                Assert.False(client.Get(Utils.Self(newObject), out result));
            }
        }


        public static JObject GetIpRestrictionsFeature(HttpClient client, string siteName, string path)
        {
            if (path != null) {
                if (!path.StartsWith("/")) {
                    throw new ArgumentException("path");
                }
            }

            string content;
            if (!client.Get(IP_RESTRICTIONS_URL + "?scope=" + siteName + path, out content)) {
                return null;
            }

            return Utils.ToJ(content);
        }
    }
}
