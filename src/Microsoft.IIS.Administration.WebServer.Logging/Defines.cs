// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Logging
{
    using Core;
    using System;

    public class Defines
    {
        private const string ENDPOINT = "logging";

        public const string LoggingName = "Microsoft.WebServer.Logging";
        public static readonly string PATH = $"{WebServer.Defines.PATH}/{ENDPOINT}";
        public static ResDef Resource = new ResDef("logging", new Guid("32964A24-BEBE-4AF5-B1F2-DEF945DC3130"), "logging");
        public const string IDENTIFIER = "log.id";
    }
}
