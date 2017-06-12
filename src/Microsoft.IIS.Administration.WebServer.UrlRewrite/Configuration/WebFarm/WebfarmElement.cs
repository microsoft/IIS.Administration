// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Web.Administration;

    sealed class WebfarmElement : ConfigurationElement {
               
        public string Name {
            get {
                return ((string)(base["name"]));
            }
            set {
                base["name"] = value;
            }
        }

        public bool Enabled {
            get {
                return ((bool)(base["enabled"]));
            }
            set {
                base["enabled"] = value;
            }
        }
    }
}

