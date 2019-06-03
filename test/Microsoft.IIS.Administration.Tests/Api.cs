// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Tests
{
    using System.Net.Http;
    using Xunit;

    public class Api
    {
        public static readonly string API_URL = $"{Configuration.Instance().TEST_SERVER_URL}/api";

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        public void CanCommunicate(int runCount)
        {
            using (HttpClient client = ApiHttpClient.Create()) {

                HttpResponseMessage res = null;

                for(int i = 0; i < runCount; i++) {

                    res = client.GetAsync(API_URL).Result;
                    Assert.True(Globals.Success(res));
                }
            }
        }
    }
}
