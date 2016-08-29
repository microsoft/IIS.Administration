// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering
{
    using System;
    using Web.Administration;

    public class ScanHeaderCollection : ConfigurationElementCollectionBase<ScanHeaderElement> {
        
        public ScanHeaderCollection() {
        }
        
        public new ScanHeaderElement this[string requestHeader] {
            get {
                for (int i = 0; (i < this.Count); i = (i + 1)) {
                    ScanHeaderElement element = base[i];
                    if ((string.Equals(element.RequestHeader, requestHeader, StringComparison.OrdinalIgnoreCase) == true)) {
                        return element;
                    }
                }
                return null;
            }
        }
        
        protected override ScanHeaderElement CreateNewElement(string elementTagName) {
            return new ScanHeaderElement();
        }
        
        public ScanHeaderElement Add(string requestHeader) {
            ScanHeaderElement element = this.CreateElement();
            element.RequestHeader = requestHeader;
            return base.Add(element);
        }
    }
}
