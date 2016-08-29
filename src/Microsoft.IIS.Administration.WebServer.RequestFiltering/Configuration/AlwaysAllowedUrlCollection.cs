// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering
{
    using System;
    using Web.Administration;

    public class AlwaysAllowedUrlCollection : ConfigurationElementCollectionBase<AlwaysAllowedUrl> {
        
        public AlwaysAllowedUrlCollection() {
        }
        
        public new AlwaysAllowedUrl this[string url] {
            get {
                for (int i = 0; (i < this.Count); i = (i + 1)) {
                    AlwaysAllowedUrl element = base[i];
                    if ((string.Equals(element.Url, url, StringComparison.OrdinalIgnoreCase) == true)) {
                        return element;
                    }
                }
                return null;
            }
        }
        
        protected override AlwaysAllowedUrl CreateNewElement(string elementTagName) {
            return new AlwaysAllowedUrl();
        }
        
        public AlwaysAllowedUrl Add(string url) {
            AlwaysAllowedUrl element = this.CreateElement();
            element.Url = url;
            return base.Add(element);
        }
    }
}
