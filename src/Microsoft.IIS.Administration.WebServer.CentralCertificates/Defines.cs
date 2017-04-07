// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.CentralCertificates
{
    using Core;
    using System;

    public class Defines
    {
        private const string ENDPOINT = "central-certificate-store";
        private const string CONNECTION_TEST_ENDPOINT = "connection-test";
        private const string CERTIFICATES_ENDPOINT = "certificates";

        public const string CentralCertsName = "Microsoft.WebServer.CentralCertificates";
        public static readonly string PATH = $"{WebServer.Defines.PATH}/{ENDPOINT}";
        public static readonly ResDef Resource = new ResDef("central_certificate_store", new Guid("7A76681D-8157-45CC-B029-704C70E262C8"), ENDPOINT);

        public const string ConnectionTestName = "Microsoft.WebServer.CentralCertificates.ConnectionTest";
        public static readonly string CONNECTION_TEST_PATH = $"{PATH}/{CONNECTION_TEST_ENDPOINT}";
        public static readonly ResDef ConnectionResource = new ResDef("connection_test", new Guid("E1A7D69F-77CD-4B36-88DA-A7D166DFBCA0"), CONNECTION_TEST_ENDPOINT);

        public const string CertificatesName = "Microsoft.WebServer.CentralCertificates.Certificates";
        public static readonly string CERTIFICATES_PATH = $"{PATH}/{CERTIFICATES_ENDPOINT}";
        public static readonly ResDef CertificatesResource = new ResDef("certificates", new Guid("D289A233-94FF-4CFE-918E-C0A090B1260F"), CERTIFICATES_ENDPOINT);

    }
}
