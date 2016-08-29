// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.DefaultDocuments
{
    using Web.Administration;

    public sealed class DefaultDocumentSection : ConfigurationSection {

        private const string FilesAttribute = "files";
        private const string EnabledAttribute = "enabled";
        
        private DefaultDocumentCollection _files;

        public DefaultDocumentSection() {
        }

        public bool Enabled {
            get {
                return (bool)base[EnabledAttribute];
            }
            set {
                base[EnabledAttribute] = value;
            }
        }

        public DefaultDocumentCollection Files {
            get {
                if (_files == null) {
                    _files = (DefaultDocumentCollection)GetCollection(FilesAttribute, typeof(DefaultDocumentCollection));
                }

                return _files;
            }
        }
    }
}
