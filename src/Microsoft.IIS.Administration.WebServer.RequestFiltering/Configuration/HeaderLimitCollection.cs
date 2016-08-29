// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering {
    using System;
    using Web.Administration;

    public class HeaderLimitCollection : ConfigurationElementCollectionBase<HeaderLimit> {
        
        public HeaderLimitCollection() {
        }
        
        public new HeaderLimit this[string header] {
            get {
                for (int i = 0; (i < this.Count); i = (i + 1)) {
                    HeaderLimit element = base[i];
                    if ((string.Equals(element.Header, header, StringComparison.OrdinalIgnoreCase) == true)) {
                        return element;
                    }
                }
                return null;
            }
        }

        public HeaderLimit Add(string header, long sizeLimit) {
            HeaderLimit element = this.CreateElement();
            element.Header = header;
            element.SizeLimit = sizeLimit;
            return base.Add(element);
        }

        protected override HeaderLimit CreateNewElement(string elementTagName) {
            return new HeaderLimit();
        }

    }
}
