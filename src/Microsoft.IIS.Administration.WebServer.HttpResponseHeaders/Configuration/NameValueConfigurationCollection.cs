// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpResponseHeaders
{
    using System;
    using Microsoft.Web.Administration;

    public sealed class NameValueConfigurationCollection : ConfigurationElementCollectionBase<NameValueConfigurationElement> {

        public NameValueConfigurationCollection() {
        }

        public new NameValueConfigurationElement this[string name] {
            get {
                for (int i = 0; i < Count; i++) {
                    NameValueConfigurationElement element = base[i];

                    if (String.Equals(element.Name, name, StringComparison.OrdinalIgnoreCase)) {
                        return element;
                    }
                }

                return null;
            }
        }

        public NameValueConfigurationElement Add(string name, string value) {
            NameValueConfigurationElement element = CreateElement();

            element.Name = name;
            element.Value = value;

            return Add(element);
        }

        protected override NameValueConfigurationElement CreateNewElement(string elementTagName) {
            return new NameValueConfigurationElement();
        }

        public void Remove(string name) {
            Remove(this[name]);
        }
    }
}
