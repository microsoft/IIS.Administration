// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Tests
{
    using System.Net.Http;
    using Xunit;
    using Xunit.Abstractions;

    public class Api
    {
        private ITestOutputHelper _output;

        public static readonly string API_URL = $"{Globals.TEST_SERVER}:{Globals.TEST_PORT}/api";

        public Api(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        public void CanCommunicate(int runCount)
        {
            using (HttpClient client = ApiHttpClient.Create($"{Globals.TEST_SERVER}:{Globals.TEST_PORT}")) {

                HttpResponseMessage res = null;

                for(int i = 0; i < runCount; i++) {

                    res = client.GetAsync(API_URL).Result;
                    Assert.True(Globals.Success(res));
                }
            }
        }
    }
}
