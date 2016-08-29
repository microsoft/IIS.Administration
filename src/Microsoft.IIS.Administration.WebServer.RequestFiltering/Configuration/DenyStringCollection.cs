// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering {
    using System;
    using Web.Administration;
    
    public class DenyStringCollection : ConfigurationElementCollectionBase<DenyStringElement> {
        
        public DenyStringCollection() {
        }
        
        public new DenyStringElement this[string @string] {
            get {
                for (int i = 0; (i < this.Count); i = (i + 1)) {
                    DenyStringElement element = base[i];
                    if ((string.Equals(element.String, @string, StringComparison.OrdinalIgnoreCase) == true)) {
                        return element;
                    }
                }
                return null;
            }
        }
        
        protected override DenyStringElement CreateNewElement(string elementTagName) {
            return new DenyStringElement();
        }
        
        public DenyStringElement Add(string @string) {
            DenyStringElement element = this.CreateElement();
            element.String = @string;
            return base.Add(element);
        }
    }
}
