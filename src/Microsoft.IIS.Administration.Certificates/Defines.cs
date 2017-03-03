// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Certificates
{
    using Core;
    using System;

    public class Defines
    {
        private const string ENDPOINT = "certificates";
        private const string EXPORTS_ENDPOINT = "exports";
        private const string IMPORTS_ENDPOINT = "imports";

        public const string Identifier = "cert.id";
        public const string CertificatesName = "Microsoft.Certificates";
        public const string CertificateName = "Microsoft.Certificate";
        public static readonly string PATH = $"{Globals.API_PATH}/{ENDPOINT}";
        public static readonly ResDef Resource = new ResDef("certificates", new Guid("C4C10AFC-3CDC-484D-9791-6D52E9E28B76"), ENDPOINT);

        public static readonly string EXPORTS_PATH = $"{PATH}/{EXPORTS_ENDPOINT}";
        public static readonly ResDef ExportsResource = new ResDef("exports", new Guid("4D16C922-51D6-450F-BD92-CEA4F0681171"), EXPORTS_ENDPOINT);

        public static readonly string IMPORTS_PATH = $"{PATH}/{IMPORTS_ENDPOINT}";
        public static readonly ResDef ImportsResource = new ResDef("imports", new Guid("09DEE7F6-DABD-4255-AA30-86E9A85D6ADD"), IMPORTS_ENDPOINT);
    }
}
