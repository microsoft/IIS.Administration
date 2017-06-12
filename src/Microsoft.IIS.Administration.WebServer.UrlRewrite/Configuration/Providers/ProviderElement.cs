// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Web.Administration;

    internal sealed class ProviderElement : ConfigurationElement {

        private SettingsCollection _settings;

        public ProviderElement() {
        }

        public string Name {
            get {
                return ((string)(base["name"]));
            }
            set {
                base["name"] = value;
            }
        }

        public string TypeName {
            get {
                return ((string)(base["type"]));
            }
            set {
                base["type"] = value;
            }
        }

        public SettingsCollection Settings {
            get {
                if ((this._settings == null)) {
                    ConfigurationElement settings = base.GetChildElement("settings");
                    this._settings = ((SettingsCollection)(settings.GetCollection(typeof(SettingsCollection))));
                }
                return this._settings;
            }
        }
    }
}

