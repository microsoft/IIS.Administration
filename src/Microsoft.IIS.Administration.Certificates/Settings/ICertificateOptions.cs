// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Certificates
{
    using System.Collections.Generic;

    public interface ICertificateOptions
    {
        void AddStore(ICertificateStoreConfiguration store);
        IEnumerable<ICertificateStoreConfiguration> Stores { get; }
    }
}
