// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using System;
    using Web.Administration;

    sealed class ProvidersCollection : ConfigurationElementCollectionBase<Provider> {

        public new Provider this[string name] {
            get {
                for (int i = 0; (i < this.Count); i = (i + 1)) {
                    Provider element = base[i];
                    if (string.Equals(element.Name, name, StringComparison.OrdinalIgnoreCase)) {
                        return element;
                    }
                }
                return null;
            }
        }

        protected override Provider CreateNewElement(string elementTagName) {
            return new Provider();
        }

        public Provider Add(string name, string typeName) {
            Provider element = this.CreateElement();
            element.Name = name;
            element.TypeName = typeName;
            return base.Add(element);
        }
    }
}

