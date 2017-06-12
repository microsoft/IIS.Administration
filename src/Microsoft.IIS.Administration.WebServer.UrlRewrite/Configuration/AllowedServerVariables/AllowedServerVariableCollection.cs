// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using System;
    using Web.Administration;

    sealed class AllowedServerVariableCollection : ConfigurationElementCollectionBase<AllowedServerVariableElement>
    {
        public new AllowedServerVariableElement this[string name] {
            get {
                for (int i = 0; i < this.Count; i++) {
                    AllowedServerVariableElement element = base[i];
                    if ((string.Equals(element.Name, name, StringComparison.OrdinalIgnoreCase) == true)) {
                        return element;
                    }
                }
                return null;
            }
        }

        protected override AllowedServerVariableElement CreateNewElement(string elementTagName)
        {
            return new AllowedServerVariableElement();
        }

        public AllowedServerVariableElement Add(string name)
        {
            AllowedServerVariableElement element = this.CreateElement();
            element.Name = name;
            return base.Add(element);
        }
    }
}

