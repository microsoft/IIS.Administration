// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Sites
{
    using Core;
    using System;

    public class Defines
    {
        private const string ENDPOINT = "websites";

        public const string WebsitesName = "Microsoft.WebServer.WebSites";
        public const string WebsiteName = "Microsoft.WebServer.WebSite";
        public static readonly string PATH = $"{WebServer.Defines.PATH}/{ENDPOINT}";
        public static readonly ResDef Resource = new ResDef("websites", new Guid("4E9A2B7A-927B-4D59-BFCA-2904AA31F721"), ENDPOINT);
        public const string IDENTIFIER = "website.id";

        internal const string WP_IDENTIFIER = "wp.id";
    }
}
