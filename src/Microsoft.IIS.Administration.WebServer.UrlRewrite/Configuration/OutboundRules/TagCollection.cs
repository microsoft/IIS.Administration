// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using System;
    using Web.Administration;

    sealed class TagCollection : ConfigurationElementCollectionBase<TagElement> {
        
        public TagElement this[string name, string attribute] {
            get {
                for (int i = 0; (i < this.Count); i = (i + 1)) {
                    TagElement element = base[i];
                    if (((string.Equals(element.Name, name, StringComparison.OrdinalIgnoreCase) == true) 
                                && (string.Equals(element.Attribute, attribute, StringComparison.OrdinalIgnoreCase) == true))) {
                        return element;
                    }
                }
                return null;
            }
        }
        
        protected override TagElement CreateNewElement(string elementTagName) {
            return new TagElement();
        }
        
        public TagElement Add(string name, string attribute) {
            TagElement element = this.CreateElement();
            element.Name = name;
            element.Attribute = attribute;
            return base.Add(element);
        }
    }
}

