// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using System;
    using Web.Administration;

    sealed class PreConditionsCollection : ConfigurationElementCollectionBase<PreConditionsElement> {

        public PreConditionsCollection() {
        }

        public new PreConditionsElement this[string name] {
            get {
                for (int i = 0; i < this.Count; i++) {
                    PreConditionsElement element = base[i];
                    if (string.Equals(element.Name, name, StringComparison.OrdinalIgnoreCase)) {
                        return element;
                    }
                }
                return null;
            }
        }

        public PreConditionsElement Add(string name) {
            PreConditionsElement element = this.CreateElement();
            element.Name = name;
            return base.Add(element);
        }

        protected override PreConditionsElement CreateNewElement(string elementTagName) {
            return new PreConditionsElement();
        }

    }
}

