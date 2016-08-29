// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Certificates
{
    using Core;
    using System;

    public class Defines
    {
        private const string ENDPOINT = "certificates";
        public const string Identifier = "cert.id";
        public const string CertificatesName = "Microsoft.Certificates";
        public const string CertificateName = "Microsoft.Certificate";

        public static readonly string PATH = $"{Globals.API_PATH}/{ENDPOINT}";
        public static readonly ResDef Resource = new ResDef("certificates", new Guid("C4C10AFC-3CDC-484D-9791-6D52E9E28B76"), ENDPOINT);
    }
}
