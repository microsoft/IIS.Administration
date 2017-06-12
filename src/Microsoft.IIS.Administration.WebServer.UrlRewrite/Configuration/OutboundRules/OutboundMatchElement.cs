// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    sealed class OutboundMatchElement : MatchElement {

        public string CustomTags {
            get {
                return ((string)(base["customTags"]));
            }
            set {
                base["customTags"] = value;
            }
        }

        public FilterByTags FilterByTags {
            get {
                return ((FilterByTags)(base["filterByTags"]));
            }
            set {
                base["filterByTags"] = ((int)(value));
            }
        }

        public string Pattern {
            get {
                return ((string)(base["pattern"]));
            }
            set {
                base["pattern"] = value;
            }
        }

        public string ServerVariable {
            get {
                return ((string)(base["serverVariable"]));
            }
            set {
                base["serverVariable"] = value;
            }
        }
    }
}

