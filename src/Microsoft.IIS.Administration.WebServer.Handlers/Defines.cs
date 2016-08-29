// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Handlers
{
    using Core;
    using System;

    public class Defines
    {
        private const string ENDPOINT = "http-handlers";
        private const string MAPPINGS_ENDPOINT = "entries";

        public const string HandlersName = "Microsoft.WebServer.Handlers";
        public static readonly string PATH = $"{WebServer.Defines.PATH}/{ENDPOINT}";
        public static readonly ResDef Resource = new ResDef("handlers", new Guid("D06CE65A-DC11-4E9F-87CF-7D04923B7C22"), ENDPOINT);
        public const string IDENTIFIER = "handler.id";

        public const string EntriesName = "Microsoft.WebServer.Handlers.Entries";
        public const string EntryName = "Microsoft.WebServer.Handlers.Entry";
        public static readonly string MAPPINGS_PATH = $"{PATH}/{MAPPINGS_ENDPOINT}";
        public static readonly ResDef MappingsResource = new ResDef("entries", new Guid("18243D38-42F6-421E-A318-841B0F7EDA62"), MAPPINGS_ENDPOINT);
        public const string MAPPINGS_IDENTIFIER = "mapping.id";
    }
}
