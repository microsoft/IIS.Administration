// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.DefaultDocuments
{
    using System;
    using Microsoft.Web.Administration;

    public sealed class DefaultDocumentCollection : ConfigurationElementCollectionBase<File> {

        public DefaultDocumentCollection() {
        }

        public new File this[string value] {
            get {
                for (int i = 0; i < Count; i++) {
                    File document = base[i];

                    if (String.Equals(document.Name, value, StringComparison.OrdinalIgnoreCase)) {
                        return document;
                    }
                }

                return null;
            }
        }
        
        public File AddAt(int index, string value) {
            File element = CreateElement();

            element.Name = value;

            return base.AddAt(index, element);
        }

        public File AddCopy(File element) {
            File newElement = CreateElement();
            CopyInfo(element, newElement);

            return Add(newElement);
        }

        public File AddCopyAt(int index, File element) {
            File newElement = CreateElement();

            CopyInfo(element, newElement);

            return AddAt(index, newElement);
        }

        private void CopyInfo(File source, File destination) {

            destination.Name = source.Name;

            object o = source.GetMetadata("lockItem");
            if (o != null) {
                destination.SetMetadata("lockItem", o);
            }

            o = source.GetMetadata("lockAttributes");
            if (o != null) {
                destination.SetMetadata("lockAttributes", o);
            }

            o = source.GetMetadata("lockElements");
            if (o != null) {
                destination.SetMetadata("lockElements", o);
            }

            o = source.GetMetadata("lockAllAttributesExcept");
            if (o != null) {
                destination.SetMetadata("lockAllAttributesExcept", o);
            }

            o = source.GetMetadata("lockAllElementsExcept");
            if (o != null) {
                destination.SetMetadata("lockAllElementsExcept", o);
            }
        }

        protected override File CreateNewElement(string elementTagName) {
            return new File();
        }

        public void Remove(string value) {
            Remove(this[value]);
        }
    }
}
