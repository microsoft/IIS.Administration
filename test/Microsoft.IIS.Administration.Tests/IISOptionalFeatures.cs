// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Tests
{
    using System.Net;
    using System.Net.Http;
    using Xunit;
    using Xunit.Abstractions;

    public class IISOptionalFeatures
    {
        private ITestOutputHelper _logger;

        public IISOptionalFeatures(ITestOutputHelper output)
        {
            _logger = output;
        }


        //[Theory]
        //[InlineData("/api/webserver/default-documents")]
        //[InlineData("/api/webserver/http-request-tracing")]
        //[InlineData("/api/webserver/authentication/basic-authentication")]
        //[InlineData("/api/webserver/authentication/digest-authentication")]
        //[InlineData("/api/webserver/authentication/windows-authentication")]
        //[InlineData("/api/webserver/authorization")]
        //[InlineData("/api/webserver/ip-restrictions")]
        //[InlineData("/api/webserver/logging")]
        //[InlineData("/api/webserver/http-request-tracing")]
        //[InlineData("/api/webserver/http-response-compression")]
        //[InlineData("/api/webserver/directory-browsing")]
        //[InlineData("/api/webserver/static-content")]
        //[InlineData("/api/webserver/http-request-filtering")]
        //[InlineData("/api/webserver/http-redirect")]
        public void InstallUninstallFeature(string feature)
        {
            _logger.WriteLine("Testing installation/uninstallation of " + feature);

            using (HttpClient client = ApiHttpClient.Create()) {
                string result;
                bool installed = IsInstalled(feature, client);

                _logger.WriteLine("Feature is initially " + (installed ? "installed" : "uninstalled"));

                if (!installed) {
                    _logger.WriteLine("Installing " + feature);
                    Assert.True(client.Post(Configuration.Instance().TEST_SERVER_URL + feature, "", out result));
                    Assert.True(IsInstalled(feature, client));
                }

                _logger.WriteLine("retrieving settings for " + feature);
                var settings = client.Get(Configuration.Instance().TEST_SERVER_URL + feature + "?scope=");

                _logger.WriteLine("Uninstalling " + feature);
                Assert.True(client.Delete(Utils.Self(settings)));
                Assert.True(!IsInstalled(feature, client));

                if (installed) {
                    _logger.WriteLine("Reinstalling " + feature);
                    Assert.True(client.Post(Configuration.Instance().TEST_SERVER_URL + feature, "", out result));
                    Assert.True(IsInstalled(feature, client));
                }
            }
        }

        private bool IsInstalled(string feature, HttpClient client)
        {
            var res = client.GetAsync(Configuration.Instance().TEST_SERVER_URL + feature + "?scope=").Result;
            return res.StatusCode != HttpStatusCode.NotFound; 
        }
    }
}
