// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Certificates
{
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;

    public interface ICertificateStoreConfiguration
    {
        string Name { get; }
        StoreLocation StoreLocation { get; }
        string Path { get; }
        IEnumerable<string> Claims { get; }
    }
}
