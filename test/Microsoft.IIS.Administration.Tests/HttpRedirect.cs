// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Tests
{
    using Newtonsoft.Json.Linq;
    using System.Net.Http;
    using Xunit;

    public class HttpRedirect
    {
        private static readonly string ENDPOINT = Configuration.Instance().TEST_SERVER_URL + "/api/webserver/http-redirect";

        [Fact]
        public void ChangeAllProperties()
        {
            using (HttpClient client = ApiHttpClient.Create()) {
                JObject serverSettings = client.Get(ENDPOINT + "?scope=");
                JObject original = (JObject) serverSettings.DeepClone();

                serverSettings["enabled"] = !serverSettings.Value<bool>("enabled");
                serverSettings["preserve_filename"] = !serverSettings.Value<bool>("preserve_filename");
                serverSettings["destination"] = "http://httpredirecttestdestination.test";
                serverSettings["absolute"] = !serverSettings.Value<bool>("absolute");
                serverSettings["status_code"] = serverSettings.Value<int>("status_code") == 302 ? 301 : 302;

                JObject newSettings = client.Patch(Utils.Self(serverSettings), serverSettings);
                Assert.NotNull(newSettings);
                try {
                    Assert.True(Utils.JEquals<bool>(serverSettings, newSettings, "enabled"));
                    Assert.True(Utils.JEquals<bool>(serverSettings, newSettings, "preserve_filename"));
                    Assert.True(Utils.JEquals<string>(serverSettings, newSettings, "destination"));
                    Assert.True(Utils.JEquals<bool>(serverSettings, newSettings, "absolute"));
                    Assert.True(Utils.JEquals<int>(serverSettings, newSettings, "status_code"));
                }
                finally {
                    newSettings = client.Patch(Utils.Self(newSettings), original);
                    Assert.NotNull(newSettings);
                }
            }
        }
    }
}
