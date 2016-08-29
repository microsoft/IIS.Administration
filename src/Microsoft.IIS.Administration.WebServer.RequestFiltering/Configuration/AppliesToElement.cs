// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering {
    using Web.Administration;
    
    public class AppliesToElement : ConfigurationElement {
        
        public AppliesToElement() {
        }
        
        public string FileExtension {
            get {
                return ((string)(base["fileExtension"]));
            }
            set {
                base["fileExtension"] = value;
            }
        }
    }
}
