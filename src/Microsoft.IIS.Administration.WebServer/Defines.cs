// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer
{
    using Core;
    using System;

    public class Defines
    {
        private const string ENDPOINT = "webserver";
        
        public const string ResourceName = "Microsoft.WebServer";
        public static readonly string PATH = $"{Globals.API_PATH}/{ENDPOINT}";
        public static readonly ResDef Resource = new ResDef("webserver", new Guid("29EAAB97-AC52-4840-B258-85C0B7125966"), ENDPOINT);
    }
}
