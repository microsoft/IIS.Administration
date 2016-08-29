// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Compression
{
    using Core;
    using System;

    public class Defines
    {
        private const string ENDPOINT = "http-response-compression";

        public const string CompressionName = "Microsoft.WebServer.Compression";
        public static readonly string PATH = $"{WebServer.Defines.PATH}/{ENDPOINT}";
        public static readonly ResDef Resource = new ResDef("response_compression", new Guid("56941C86-B969-48B6-9042-FF2883A9A14A"), ENDPOINT);
        public const string IDENTIFIER = "comp.id";
    }
}
