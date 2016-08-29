// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering
{
    using System;
    using Web.Administration;
    
    public class AlwaysAllowedQueryStringCollection : ConfigurationElementCollectionBase<AlwaysAllowedQueryStringElement> {
        
        public AlwaysAllowedQueryStringCollection() {
        }
        
        public new AlwaysAllowedQueryStringElement this[string queryString] {
            get {
                for (int i = 0; (i < this.Count); i = (i + 1)) {
                    AlwaysAllowedQueryStringElement element = base[i];
                    if ((string.Equals(element.QueryString, queryString, StringComparison.OrdinalIgnoreCase) == true)) {
                        return element;
                    }
                }
                return null;
            }
        }
        
        protected override AlwaysAllowedQueryStringElement CreateNewElement(string elementTagName) {
            return new AlwaysAllowedQueryStringElement();
        }
        
        public AlwaysAllowedQueryStringElement Add(string queryString) {
            AlwaysAllowedQueryStringElement element = this.CreateElement();
            element.QueryString = queryString;
            return base.Add(element);
        }
    }
}
