// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.StaticContent
{
    using System;
    using Microsoft.Web.Administration;

    public sealed class MimeMapCollection : ConfigurationElementCollectionBase<MimeMap>
    {

        public MimeMapCollection() {
        }

        public new MimeMap this[string fileExtension] {
            get {
                for (int i = 0; i < Count; i++) {
                    MimeMap element = base[i];

                    if (String.Equals(element.FileExtension, fileExtension, StringComparison.OrdinalIgnoreCase)) {
                        return element;
                    }
                }

                return null;
            }
        }

        public MimeMap Add(string fileExtension, string mimeType) {
            MimeMap element = CreateElement();

            element.FileExtension = fileExtension;
            element.MimeType = mimeType;

            return Add(element);
        }

        protected override MimeMap CreateNewElement(string elementTagName) {
            return new MimeMap();
        }

        public void Remove(string fileExtension) {
            base.Remove(this[fileExtension]);
        }
    }
}
