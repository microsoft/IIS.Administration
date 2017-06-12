// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Web.Administration;

    internal sealed class KeyValueElement : ConfigurationElement {
        
        public string Key {
            get {
                return ((string)(base["key"]));
            }
            set {
                base["key"] = value;
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

