// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Web.Administration;

    sealed class ProxySection : ConfigurationSection {

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

