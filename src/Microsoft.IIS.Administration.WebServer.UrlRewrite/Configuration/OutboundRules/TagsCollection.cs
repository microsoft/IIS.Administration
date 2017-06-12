// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using System;
    using Web.Administration;

    sealed class TagsCollection : ConfigurationElementCollectionBase<TagsElement> {

        public new TagsElement this[string name] {
            get {
                for (int i = 0; (i < this.Count); i = (i + 1)) {
                    TagsElement element = base[i];
                    if (string.Equals(element.Name, name, StringComparison.OrdinalIgnoreCase)) {
                        return element;
                    }
                }
                return null;
            }
        }

        protected override TagsElement CreateNewElement(string elementTagName) {
            return new TagsElement();
        }

        public TagsElement Add(string name) {
            TagsElement element = this.CreateElement();
            element.Name = name;
            return base.Add(element);
        }
    }
}

