// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Certificates
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    public interface ICertificateStore
    {
        string Name { get; }

        IEnumerable<string> Claims { get; }

        Task<IEnumerable<ICertificate>> GetCertificates();

        Task<ICertificate> GetCertificate(string id);

        Stream GetContent(ICertificate certificate, bool persistKey, string password);
    }
}
