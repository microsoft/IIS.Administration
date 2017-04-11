// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer
{
    using Certificates;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;

    class CertStore : ICertificateStoreConfiguration
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public IEnumerable<string> Claims { get; set; }

        public StoreLocation StoreLocation {
            get {
                return StoreLocation.LocalMachine;
            }
        }
    }
}
