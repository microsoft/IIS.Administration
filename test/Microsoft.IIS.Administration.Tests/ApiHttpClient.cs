// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Tests
{
    using Core.Http;
    using System.Net.Http;
    using System.Net.Security;
    using System.Security.Cryptography.X509Certificates;

    public class ApiHttpClient : HttpClient
    {
        private string _keyId;
        private string _serverUri;
        private HttpClient _keyClient;

        public static HttpClient Create()
        {
            return Create(Configuration.TEST_SERVER_URL);
        }

        public static HttpClient Create(string serverUri)
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = ServerCertificateCustomValidationCallback;
            return new ApiHttpClient(serverUri, handler, true);
        }

        private ApiHttpClient(string serverUri, HttpMessageHandler handler, bool disposeHandler) : base(handler, disposeHandler)
        {
            _keyClient = new HttpClient(new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = ServerCertificateCustomValidationCallback,
                UseDefaultCredentials = true
            }, true);
            Init(serverUri);
        }

        private void Init(string serverUri)
        {
            var key = Utils.GetApiKey(serverUri, _keyClient);
            _keyId = key.Value<string>("id");
            _serverUri = serverUri;

            this.DefaultRequestHeaders.Add(HeaderNames.Access_Token, "Bearer " + key.Value<string>("access_token"));
            this.DefaultRequestHeaders.Add("Accept", "application/hal+json");
        }

        private static bool ServerCertificateCustomValidationCallback(HttpRequestMessage msg, X509Certificate2 cert, X509Chain x509, SslPolicyErrors errors)
        {
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                this.DefaultRequestHeaders.Clear();
                Utils.DeleteApiKey(_serverUri, _keyId, _keyClient);
            }
            finally
            {
                if (_keyClient != null)
                {
                    _keyClient.Dispose();
                }
                base.Dispose(disposing);
            }
        }
    }
}
