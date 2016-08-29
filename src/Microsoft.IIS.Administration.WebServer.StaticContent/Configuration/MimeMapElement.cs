// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.StaticContent
{
    using Microsoft.Web.Administration;

    public sealed class MimeMap : ConfigurationElement {

        private const string FileExtensionAttribute = "fileExtension";
        private const string MimeTypeAttribute = "mimeType";

        public MimeMap() {
        }

        public string FileExtension {
            get {
                return (string)base[FileExtensionAttribute];
            }
            set {
                base[FileExtensionAttribute] = value;
            }
        }

        public string MimeType {
            get {
                return (string)base[MimeTypeAttribute];
            }
            set {
                base[MimeTypeAttribute] = value;
            }
        }
    }
}
