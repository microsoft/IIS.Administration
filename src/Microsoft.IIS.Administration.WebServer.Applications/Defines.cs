// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Applications
{
    using Core;
    using System;

    public class Defines
    {
        private const string ENDPOINT = "webapps";

        public const string WebAppsName = "Microsoft.WebServer.WebApps";
        public const string WebAppName = "Microsoft.WebServer.WebApp";
        public static readonly string PATH = $"{WebServer.Defines.PATH}/{ENDPOINT}";
        public static readonly ResDef Resource = new ResDef("webapps", new Guid("9A1FB34E-6A66-4847-BC44-B79A4DEF35BC"), ENDPOINT);

        public const string IDENTIFIER = "webapp.id";
        internal const string WP_IDENTIFIER = "wp.id";
    }
}
