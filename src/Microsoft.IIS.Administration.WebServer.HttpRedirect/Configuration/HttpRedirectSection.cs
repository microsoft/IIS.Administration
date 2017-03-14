// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRedirect
{
    using Web.Administration;

    class HttpRedirectSection : ConfigurationSection
    {
        public const string HttpRedirectSectionName = "system.webServer/httpRedirect";

        private const string EnabledAttribute = "enabled";
        private const string HttpResponseStatusAttribute = "httpResponseStatus";
        private const string DestinationAttribute = "destination";
        private const string ExactDestinationAttribute = "exactDestination";
        private const string ChildOnlyAttribute = "childOnly";

        public HttpRedirectSection() { }

        public bool ChildOnly {
            get {
                return (bool)base[ChildOnlyAttribute];
            }
            set {
                base[ChildOnlyAttribute] = value;
            }
        }

        public string Destination {
            get {
                return (string)base[DestinationAttribute];
            }
            set {
                base[DestinationAttribute] = value;
            }
        }

        public bool Enabled {
            get {
                return (bool)base[EnabledAttribute];
            }
            set {
                base[EnabledAttribute] = value;
            }
        }

        public bool ExactDestination {
            get {
                return (bool)base[ExactDestinationAttribute];
            }
            set {
                base[ExactDestinationAttribute] = value;
            }
        }

        public RedirectHttpResponseStatus HttpResponseStatus {
            get {
                return (RedirectHttpResponseStatus)base[HttpResponseStatusAttribute];
            }
            set {
                base[HttpResponseStatusAttribute] = (int)value;
            }
        }
    }
}
