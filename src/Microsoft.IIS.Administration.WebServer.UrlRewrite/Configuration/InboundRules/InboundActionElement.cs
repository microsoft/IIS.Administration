// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    sealed class InboundActionElement : ActionElement {

        public bool AppendQueryString {
            get {
                return ((bool)(base["appendQueryString"]));
            }
            set {
                base["appendQueryString"] = value;
            }
        }

        public bool LogRewrittenUrl {
            get {
                return ((bool)(base["logRewrittenUrl"]));
            }
            set {
                base["logRewrittenUrl"] = value;
            }
        }

        public RedirectType RedirectType {
            get {
                return ((RedirectType)(base["redirectType"]));
            }
            set {
                base["redirectType"] = ((int)(value));
            }
        }

        public long StatusCode {
            get {
                return (long)(base["statusCode"]);
            }
            set {
                base["statusCode"] = value;
            }
        }

        public string StatusDescription {
            get {
                return ((string)(base["statusDescription"]));
            }
            set {
                base["statusDescription"] = value;
            }
        }

        public string StatusReason {
            get {
                return ((string)(base["statusReason"]));
            }
            set {
                base["statusReason"] = value;
            }
        }

        public long SubStatusCode {
            get {
                return (long)(base["subStatusCode"]);
            }
            set {
                base["subStatusCode"] = value;
            }
        }

        public string Url {
            get {
                return ((string)(base["url"]));
            }
            set {
                base["url"] = value;
            }
        }

        internal void CopyTo(InboundActionElement destination) {
            ConfigurationHelper.CopyAttributes(this, destination);
        }
    }
}

