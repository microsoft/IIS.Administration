// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Web.Administration;

    class MatchElement : ConfigurationElement {
        
        public bool IgnoreCase {
            get {
                return ((bool)(base["ignoreCase"]));
            }
            set {
                base["ignoreCase"] = value;
            }
        }
        
        public bool Negate {
            get {
                return ((bool)(base["negate"]));
            }
            set {
                base["negate"] = value;
            }
        }
       
        internal void CopyTo(MatchElement destination) {
            ConfigurationHelper.CopyAttributes(this, destination);
        }
    }
}

