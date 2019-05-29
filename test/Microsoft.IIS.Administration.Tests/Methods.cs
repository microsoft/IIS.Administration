// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Net.Http;
using Xunit;

namespace Microsoft.IIS.Administration.Tests
{
    public class Methods
    {
        [Theory]
        [InlineData("/api/webserver/websites")]
        [InlineData("/api/webserver/application-pools")]
        public void Head(string resourceEndpoint)
        {
            using (HttpClient client = ApiHttpClient.Create()) {

                var req = new HttpRequestMessage(new HttpMethod("GET"), $"{Configuration.Instance().TEST_SERVER_URL}{resourceEndpoint}");

                var res = client.SendAsync(req).Result;

                var getContent = res.Content.ReadAsStringAsync().Result;

                Assert.NotEqual(getContent, string.Empty);

                req = new HttpRequestMessage(new HttpMethod("HEAD"), $"{Configuration.Instance().TEST_SERVER_URL}{resourceEndpoint}");

                res = client.SendAsync(req).Result;

                var headContent = res.Content.ReadAsStringAsync().Result;

                Assert.Equal(headContent, string.Empty);
            }
        }
    }
}
