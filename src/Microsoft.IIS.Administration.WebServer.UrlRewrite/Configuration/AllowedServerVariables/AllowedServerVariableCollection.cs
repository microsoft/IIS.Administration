// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using System;
    using Web.Administration;

    sealed class AllowedServerVariableCollection : ConfigurationElementCollectionBase<AllowedServerVariable>
    {
        public new AllowedServerVariable this[string name] {
            get {
                for (int i = 0; i < this.Count; i++) {
                    AllowedServerVariable element = base[i];
                    if ((string.Equals(element.Name, name, StringComparison.OrdinalIgnoreCase) == true)) {
                        return element;
                    }
                }
                return null;
            }
        }

        protected override AllowedServerVariable CreateNewElement(string elementTagName)
        {
            return new AllowedServerVariable();
        }

        public AllowedServerVariable Add(string name)
        {
            AllowedServerVariable element = this.CreateElement();
            element.Name = name;
            return base.Add(element);
        }
    }
}

