// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering {
    using System;
    using Web.Administration;
    
    public class AppliesToCollection : ConfigurationElementCollectionBase<AppliesToElement> {
        
        public AppliesToCollection() {
        }
        
        public new AppliesToElement this[string fileExtension] {
            get {
                for (int i = 0; (i < this.Count); i = (i + 1)) {
                    AppliesToElement element = base[i];
                    if ((string.Equals(element.FileExtension, fileExtension, StringComparison.OrdinalIgnoreCase) == true)) {
                        return element;
                    }
                }
                return null;
            }
        }
        
        protected override AppliesToElement CreateNewElement(string elementTagName) {
            return new AppliesToElement();
        }
        
        public AppliesToElement Add(string fileExtension) {
            AppliesToElement element = this.CreateElement();
            element.FileExtension = fileExtension;
            return base.Add(element);
        }
    }
}
