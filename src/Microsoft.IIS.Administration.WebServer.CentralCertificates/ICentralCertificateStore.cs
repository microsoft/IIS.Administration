// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.CentralCertificates
{
    using Certificates;
    using System.Threading.Tasks;

    public interface ICentralCertificateStore
    {
        Task<ICertificate> GetCertificateByHostName(string name);
    }
}
