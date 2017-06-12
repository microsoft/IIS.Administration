// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Web.Administration;

    sealed class RewriteMapElement : ConfigurationElement {
        
        private KeyValueCollection _keyValuePairCollection;

        public string DefaultValue {
            get {
                return ((string)(base["defaultValue"]));
            }
            set {
                base["defaultValue"] = value;
            }
        }
        
        public bool IgnoreCase {
            get {
                return ((bool)(base["ignoreCase"]));
            }
            set {
                base["ignoreCase"] = value;
            }
        }

        public KeyValueCollection KeyValuePairCollection {
            get {
                if ((this._keyValuePairCollection == null)) {
                    this._keyValuePairCollection = ((KeyValueCollection)(base.GetCollection(typeof(KeyValueCollection))));
                }
                return this._keyValuePairCollection;
            }
        }

        public string Name {
            get {
                return ((string)(base["name"]));
            }
            set {
                base["name"] = value;
            }
        }

    }
}

