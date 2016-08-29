// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering
{
    using System;
    using Web.Administration;

    public class FileExtensionCollection : ConfigurationElementCollectionBase<Extension> {
        
        public FileExtensionCollection() {
        }

        public bool AllowUnlisted {
            get {
                return ((bool)(base["allowUnlisted"]));
            }
            set {
                base["allowUnlisted"] = value;
            }
        }

        public new Extension this[string fileExtension] {
            get {
                for (int i = 0; (i < this.Count); i = (i + 1)) {
                    Extension element = base[i];
                    if ((string.Equals(element.FileExtension, fileExtension, StringComparison.OrdinalIgnoreCase) == true)) {
                        return element;
                    }
                }
                return null;
            }
        }

        public Extension Add(string fileExtension, bool allowed) {
            Extension element = this.CreateElement();
            element.FileExtension = fileExtension;
            element.Allowed = allowed;
            return base.Add(element);
        }

        protected override Extension CreateNewElement(string elementTagName) {
            return new Extension();
        }

    }
}
