// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering
{
    using Web.Administration;

    public class AlwaysAllowedUrl : ConfigurationElement {
        
        public AlwaysAllowedUrl() {
        }
        
        public string Url {
            get {
                return ((string)(base["url"]));
            }
            set {
                base["url"] = value;
            }
        }
    }
}
