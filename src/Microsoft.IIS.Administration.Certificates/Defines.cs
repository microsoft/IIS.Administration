// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Certificates
{
    using Core;
    using System;

    public class Defines
    {
        private const string ENDPOINT = "certificates";
        private const string STORES_ENDPOINT = "stores";
        private const string EXPORTS_ENDPOINT = "exports";
        private const string IMPORTS_ENDPOINT = "imports";

        public const string Identifier = "cert.id";
        public const string CertificatesName = "Microsoft.Certificates";
        public const string CertificateName = "Microsoft.Certificate";
        public static readonly string PATH = $"{Globals.API_PATH}/{ENDPOINT}";
        public static readonly ResDef Resource = new ResDef("certificates", new Guid("C4C10AFC-3CDC-484D-9791-6D52E9E28B76"), ENDPOINT);

        public const string StoreIdentifier = "certificate_store.id";
        public const string StoresName = "Microsoft.Certificates.Stores";
        public const string StoreName = "Microsoft.Certificate.Store";
        public static readonly string STORES_PATH = $"{PATH}/{STORES_ENDPOINT}";
        public static readonly ResDef StoresResource = new ResDef("certificate_stores", new Guid("42FEB52E-896F-4769-AB1D-D924F3617434"), STORES_ENDPOINT);
    }
}
