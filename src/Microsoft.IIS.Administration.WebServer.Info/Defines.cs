// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Info
{
    using Core;
    using System;

    public class Defines
    {
        private const string ENDPOINT = "info";

        public const string InfoName = "Microsoft.WebServer.Info";
        public static readonly string PATH = $"{WebServer.Defines.PATH}/{ENDPOINT}";
        public static readonly ResDef Resource = new ResDef("info", new Guid("FB9F36C4-91EC-4239-8338-FBBFF0F97B17"), ENDPOINT);
    }
}
