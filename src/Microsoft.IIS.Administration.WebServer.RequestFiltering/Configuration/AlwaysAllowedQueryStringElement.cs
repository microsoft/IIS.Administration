// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering {
    using Web.Administration;

    public class AlwaysAllowedQueryStringElement : ConfigurationElement {
        
        public AlwaysAllowedQueryStringElement() {
        }
        
        public string QueryString {
            get {
                return ((string)(base["queryString"]));
            }
            set {
                base["queryString"] = value;
            }
        }
    }
}
