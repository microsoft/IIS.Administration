// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Tests
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Net.Http;
    using Xunit;

    public class StaticContent
    {
        public static readonly string STATIC_CONTENT_URL = $"{Configuration.Instance().TEST_SERVER_URL}/api/webserver/static-content";

        [Fact]
        public void ChangeAllProperties()
        {
            string defaultDocFooterTestValue = "test_str";
            string clientCacheControlModeTestValue = "use_max_age";
            string clientCacheControlCustomTestValue = "test_str_control_cust";

            using (HttpClient client = ApiHttpClient.Create()) {

                JObject staticContent = GetStaticContentFeature(client, null, null);
                JObject cachedFeature = new JObject(staticContent);

                staticContent["default_doc_footer"] = defaultDocFooterTestValue;
                staticContent["is_doc_footer_file_name"] = !staticContent.Value<bool>("is_doc_footer_file_name");
                staticContent["enable_doc_footer"] = !staticContent.Value<bool>("enable_doc_footer");

                JObject clientCache = staticContent.Value<JObject>("client_cache");
                clientCache["max_age"] = clientCache.Value<long>("max_age") == 0 ? 1 : clientCache.Value<long>("max_age") - 1;
                clientCache["control_mode"] = clientCacheControlModeTestValue;
                clientCache["control_custom"] = clientCacheControlCustomTestValue;
                clientCache["set_e_tag"] = !clientCache.Value<bool>("set_e_tag");

                string result;
                string body = JsonConvert.SerializeObject(staticContent);

                Assert.True(client.Patch(Utils.Self(staticContent), body, out result));

                JObject newStaticContent = JsonConvert.DeserializeObject<JObject>(result);

                Assert.True(Utils.JEquals<string>(staticContent, newStaticContent, "default_doc_footer"));
                Assert.True(Utils.JEquals<bool>(staticContent, newStaticContent, "is_doc_footer_file_name"));
                Assert.True(Utils.JEquals<bool>(staticContent, newStaticContent, "enable_doc_footer"));
                Assert.True(Utils.JEquals<long>(staticContent, newStaticContent, "client_cache.max_age"));
                Assert.True(Utils.JEquals<string>(staticContent, newStaticContent, "client_cache.control_mode"));
                Assert.True(Utils.JEquals<string>(staticContent, newStaticContent, "client_cache.control_custom"));
                Assert.True(Utils.JEquals<bool>(staticContent, newStaticContent, "client_cache.set_e_tag"));

                // Create json payload of original feature state
                body = JsonConvert.SerializeObject(cachedFeature);

                // Patch request filtering to original state
                Assert.True(client.Patch(Utils.Self(newStaticContent), body, out result));
            }
        }

        [Fact]
        public void CreateRemoveMimeMap()
        {
            using (HttpClient client = ApiHttpClient.Create()) {

                JObject feature = GetStaticContentFeature(client, null, null);

                string link = Utils.GetLink(feature, "mime_maps");

                var mimeMapPayload = new {
                    // Want to avoid collisions with any default file extensions
                    file_extension = "tst9",
                    mime_type = "test/test",
                    static_content = feature
                };

                string result;
                Assert.True(client.Post(link, JsonConvert.SerializeObject(mimeMapPayload), out result));

                JObject mimeMap = JsonConvert.DeserializeObject<JObject>(result);

                Assert.True(client.Delete(Utils.Self(mimeMap)));
                Assert.False(client.Get(Utils.Self(mimeMap), out result));
            }
        }


        public static JObject GetStaticContentFeature(HttpClient client, string siteName, string path)
        {
            if (path != null) {
                if (!path.StartsWith("/")) {
                    throw new ArgumentException("path");
                }
            }

            string content;
            if (!client.Get(STATIC_CONTENT_URL + "?scope=" + siteName + path, out content)) {
                return null;
            }

            return Utils.ToJ(content);
        }
    }
}
