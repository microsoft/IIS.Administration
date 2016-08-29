// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering {
    using Web.Administration;

    public class RequestLimitsElement : ConfigurationElement {
        
        private HeaderLimitCollection _headerLimits;
        
        public RequestLimitsElement() {
        }

        public HeaderLimitCollection HeaderLimits {
            get {
                if ((this._headerLimits == null)) {
                    this._headerLimits = ((HeaderLimitCollection)(base.GetCollection("headerLimits", typeof(HeaderLimitCollection))));
                }
                return this._headerLimits;
            }
        }

        public long MaxAllowedContentLength {
            get {
                return ((long)(base["maxAllowedContentLength"]));
            }
            set {
                base["maxAllowedContentLength"] = value;
            }
        }
        
        public long MaxQueryString {
            get {
                return ((long)(base["maxQueryString"]));
            }
            set {
                base["maxQueryString"] = value;
            }
        }
        
        public long MaxUrl {
            get {
                return ((long)(base["maxUrl"]));
            }
            set {
                base["maxUrl"] = value;
            }
        }

    }
}
