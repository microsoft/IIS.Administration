// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Tests
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Net.Http;
    using Xunit;

    public class DirectoryBrowsing
    {
        private const string TEST_SITE_NAME = "dirbro_test_site";

        public static readonly string DIRECTORY_BROWSING_URL = $"{Configuration.TEST_SERVER_URL}/api/webserver/directory-browsing";

        [Fact]
        public void ChangeAllProperties()
        {
            using (HttpClient client = ApiHttpClient.Create()) {

                Sites.EnsureNoSite(client, TEST_SITE_NAME);

                JObject site = Sites.CreateSite(client, TEST_SITE_NAME, 50310, @"c:\sites\test_site");
                JObject feature = GetDirectoryBrowsingFeatrue(client, site.Value<string>("name"), null);
                Assert.NotNull(feature);

                JObject cachedFeature = new JObject(feature);

                feature["enabled"] = !feature.Value<bool>("enabled");

                var allowedAttributes = feature.Value<JObject>("allowed_attributes");
                allowedAttributes["date"] = !allowedAttributes.Value<bool>("date");
                allowedAttributes["time"] = !allowedAttributes.Value<bool>("time");
                allowedAttributes["size"] = !allowedAttributes.Value<bool>("size");
                allowedAttributes["extension"] = !allowedAttributes.Value<bool>("extension");
                allowedAttributes["long_date"] = !allowedAttributes.Value<bool>("long_date");

                string result;
                string body = JsonConvert.SerializeObject(feature);

                Assert.True(client.Patch(Utils.Self(feature), body, out result));

                JObject newFeature = JsonConvert.DeserializeObject<JObject>(result);

                Assert.True(Utils.JEquals<bool>(feature, newFeature, "enabled"));
                Assert.True(Utils.JEquals<bool>(feature, newFeature, "allowed_attributes.date"));
                Assert.True(Utils.JEquals<bool>(feature, newFeature, "allowed_attributes.time"));
                Assert.True(Utils.JEquals<bool>(feature, newFeature, "allowed_attributes.size"));
                Assert.True(Utils.JEquals<bool>(feature, newFeature, "allowed_attributes.extension"));
                Assert.True(Utils.JEquals<bool>(feature, newFeature, "allowed_attributes.long_date"));

                body = JsonConvert.SerializeObject(cachedFeature);
                Assert.True(client.Patch(Utils.Self(newFeature), body, out result));

                Sites.EnsureNoSite(client, TEST_SITE_NAME);
            }
        }

        public static JObject GetDirectoryBrowsingFeatrue(HttpClient client, string siteName, string path)
        {
            if (path != null) {
                if (!path.StartsWith("/")) {
                    throw new ArgumentException("path");
                }
            }

            string content;
            if (!client.Get(DIRECTORY_BROWSING_URL + "?scope=" + siteName + path, out content)) {
                return null;
            }

            return Utils.ToJ(content);
        }
    }
}
