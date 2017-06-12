// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using System;
    using Web.Administration;

    internal sealed class KeyValueCollection : ConfigurationElementCollectionBase<KeyValueElement> {

        public KeyValueCollection() {
        }

        public KeyValueElement GetItem(string name, bool ignoreCase) {
            for (int i = 0; i < Count; i++) {
                KeyValueElement element = base[i];
                if (ignoreCase) {
                    if (String.Equals(element.Key, name, StringComparison.OrdinalIgnoreCase)) {
                        return element;
                    }
                }
                else {
                    if (String.Equals(element.Key, name, StringComparison.Ordinal)) {
                        return element;
                    }
                }
            }
            return null;
        }

        protected override KeyValueElement CreateNewElement(string elementTagName) {
            return new KeyValueElement();
        }

    }
}

