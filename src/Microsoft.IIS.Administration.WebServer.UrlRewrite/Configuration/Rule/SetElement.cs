// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Web.Administration;

    sealed class ServerVariableAssignment : ConfigurationElement {
        
        public string Name {
            get {
                return ((string)(base["name"]));
            }
            set {
                base["name"] = value;
            }
        }

        public bool Replace {
            get {
                return ((bool)(base["replace"]));
            }
            set {
                base["replace"] = value;
            }
        }

        public string Value {
            get {
                return ((string)(base["value"]));
            }
            set {
                base["value"] = value;
            }
        }
    }
}

