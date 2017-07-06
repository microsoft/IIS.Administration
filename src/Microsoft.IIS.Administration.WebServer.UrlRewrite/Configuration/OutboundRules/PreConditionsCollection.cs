// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using System;
    using Web.Administration;

    sealed class PreConditionCollection : ConfigurationElementCollectionBase<PreCondition> {

        public PreConditionCollection() {
        }

        public new PreCondition this[string name] {
            get {
                for (int i = 0; i < this.Count; i++) {
                    PreCondition element = base[i];
                    if (string.Equals(element.Name, name, StringComparison.OrdinalIgnoreCase)) {
                        return element;
                    }
                }
                return null;
            }
        }

        public PreCondition Add(string name) {
            PreCondition element = this.CreateElement();
            element.Name = name;
            return base.Add(element);
        }

        protected override PreCondition CreateNewElement(string elementTagName) {
            return new PreCondition();
        }

    }
}

