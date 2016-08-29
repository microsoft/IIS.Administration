// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.AppPools
{
    using Core;
    using System;

    public class Defines
    {
        private const string ENDPOINT = "application-pools";
        public const string IDENTIFIER = "application_pool.id";

        public const string AppPoolsName = "Microsoft.WebServer.AppPools";
        public const string AppPoolName = "Microsoft.WebServer.AppPool";
        public static readonly string PATH = $"{WebServer.Defines.PATH}/{ENDPOINT}";

        public static readonly ResDef Resource = new ResDef("app_pools", new Guid("E67CD594-0E06-4531-BC0D-45428EB9E151"), ENDPOINT);
    }
}
