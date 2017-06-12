// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using System;
    using Web.Administration;

    sealed class WebfarmCollection : ConfigurationElementCollectionBase<WebfarmElement> {

        public new WebfarmElement this[string name] {
            get {
                for (int i = 0; (i < this.Count); i = (i + 1)) {
                    WebfarmElement element = base[i];
                    if ((string.Equals(element.Name, name, StringComparison.OrdinalIgnoreCase) == true)) {
                        return element;
                    }
                }
                return null;
            }
        }
    }
}

