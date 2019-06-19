// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Certificates
{
    using System;
    using System.Collections.Generic;

    public interface ICertificate
    {
        string Alias { get; }
        string Id { get; }
        string Thumbprint { get; }
        string Issuer { get; }
        string Subject { get; }
        string SignatureAlgorithm { get; }
        string SignatureAlgorithmOID { get; }
        IEnumerable<string> SubjectAlternativeNames { get; }
        DateTime Expires { get; }
        DateTime ValidFrom { get; }
        int Version { get; }
        IEnumerable<string> Purposes { get; }
        IEnumerable<string> PurposesOID { get; }
        ICertificateStore Store { get; }
        bool HasPrivateKey { get; }
        bool IsPrivateKeyExportable { get; }
    }
}
