// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Tests
{
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using Xunit;

    public class IISOptionalFeatures
    {
        [Fact]
        public void InstallUninstallFeature()
        {
            var features = new List<Feature>() {
                new Feature() {
                    Name = "IIS-DefaultDocument",
                    Module = "DefaultDocumentModule",
                    Endpoint = "/api/webserver/default-documents"
                }
            };


            using (HttpClient client = ApiHttpClient.Create()) {
                string result;
                foreach (var feature in features) {
                    bool installed = IsInstalled(feature, client);

                    if (!installed) {
                        Assert.True(client.Post(Configuration.TEST_SERVER_URL + feature.Endpoint, "", out result));
                    }

                    var settings = client.Get(Configuration.TEST_SERVER_URL + feature.Endpoint + "?scope=");
                    Assert.True(client.Delete(Utils.Self(settings)));

                    if (installed) {
                        Assert.True(client.Post(Configuration.TEST_SERVER_URL + feature.Endpoint, "", out result));
                    }
                }
            }
        }

        private bool IsInstalled(Feature feature, HttpClient client)
        {
            var res = client.GetAsync(Configuration.TEST_SERVER_URL + feature.Endpoint + "?scope=").Result;
            return res.StatusCode != HttpStatusCode.NotFound; 
        }

        private class Feature
        {
            public string Name { get; set; }
            public string Module { get; set; }
            public string Endpoint { get; set; }
        }
    }
}
