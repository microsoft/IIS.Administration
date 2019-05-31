// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Tests
{
    using System;
    using System.Net;
    using System.Net.Http;
    using Xunit;

    public class Cors
    {
        [Fact]
        public void EnsureCors()
        {
            using (HttpClient client = ApiHttpClient.Create()) {

                HttpRequestMessage message = new HttpRequestMessage(new HttpMethod("OPTIONS"), $"{Configuration.Instance().TEST_SERVER_URL}/api");

                message.Headers.Add("Access-Control-Request-Headers", "X-PINGOTHER");
                message.Headers.Add("Access-Control-Request-Method", "GET");
                
                message.Headers.Add("Origin", "https://manage.iis.net");

                HttpResponseMessage res = client.SendAsync(message).Result;

                Assert.True(res.StatusCode == HttpStatusCode.NoContent);
                Assert.True(res.Headers.Contains("Access-Control-Allow-Origin"));
            }
        }
    }
}
