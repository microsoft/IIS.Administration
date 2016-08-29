// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering {
    using Web.Administration;
    
    public class DenyStringElement : ConfigurationElement {
        
        public DenyStringElement() {
        }
        
        public string String {
            get {
                return ((string)(base["string"]));
            }
            set {
                base["string"] = value;
            }
        }
    }
}
