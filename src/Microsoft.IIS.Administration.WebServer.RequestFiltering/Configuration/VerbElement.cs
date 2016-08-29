// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering
{
    using Web.Administration;

    public class VerbElement : ConfigurationElement {
        
        public VerbElement() {
        }
        
        public bool Allowed {
            get {
                return ((bool)(base["allowed"]));
            }
            set {
                base["allowed"] = value;
            }
        }
        
        public string Verb {
            get {
                return ((string)(base["verb"]));
            }
            set {
                base["verb"] = value;
            }
        }
    }
}
