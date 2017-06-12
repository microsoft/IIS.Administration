// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using System;
    using Web.Administration;

    internal sealed class SettingsCollection : ConfigurationElementCollectionBase<SettingElement> {

        public SettingsCollection() {
        }

        public new SettingElement this[string key] {
            get {
                for (int i = 0; i < this.Count; i++) {
                    SettingElement element = base[i];
                    if (string.Equals(element.Key, key, StringComparison.OrdinalIgnoreCase) == true) {
                        return element;
                    }
                }
                return null;
            }
        }

        protected override SettingElement CreateNewElement(string elementTagName) {
            return new SettingElement();
        }

        public SettingElement Add(string key, string value, bool encrypted) {
            SettingElement element = this.CreateElement();
            element.Key = key;
            if (encrypted) {
                element.EncryptedValue = value;
            }
            else {
                element.Value = value;
            }
            return base.Add(element);
        }
    }
}

