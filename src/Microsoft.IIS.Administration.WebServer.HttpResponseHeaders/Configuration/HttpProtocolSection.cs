// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpResponseHeaders
{
    using Microsoft.Web.Administration;

    public sealed class HttpProtocolSection : ConfigurationSection {

        private const string AllowKeepAliveAttribute = "allowKeepAlive";
        private const string CustomHeadersAttribute = "customHeaders";
        private const string RedirectHeadersAttribute = "redirectHeaders";

        private NameValueConfigurationCollection _customHeadersCollection;
        private NameValueConfigurationCollection _redirectHeadersCollection;

        public HttpProtocolSection() {
        }

        public bool AllowKeepAlive {
            get {
                return (bool)base[AllowKeepAliveAttribute];
            }
            set {
                base[AllowKeepAliveAttribute] = value;
            }
        }

        public NameValueConfigurationCollection CustomHeaders {
            get {
                if (_customHeadersCollection == null) {
                    _customHeadersCollection = (NameValueConfigurationCollection)GetCollection(CustomHeadersAttribute, typeof(NameValueConfigurationCollection));
                }

                return _customHeadersCollection;
            }
        }

        public NameValueConfigurationCollection RedirectHeaders {
            get {
                if (_redirectHeadersCollection == null) {
                    _redirectHeadersCollection = (NameValueConfigurationCollection)GetCollection(RedirectHeadersAttribute, typeof(NameValueConfigurationCollection));
                }

                return _redirectHeadersCollection;
            }
        }
    }
}
