// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.DefaultDocuments
{
    using Web.Administration;

    public sealed class File : ConfigurationElement {

        private const string ValueAttribute = "value";

        public File() {
        }
        
        public string Name {
            get {
                return (string)base[ValueAttribute];
            }
            set {
                base[ValueAttribute] = value;
            }
        }
    }
}
