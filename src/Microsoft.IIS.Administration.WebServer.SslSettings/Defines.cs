// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.SslSettings
{
    using Core;
    using System;

    public class Defines
    {
        private const string ENDPOINT = "ssl-settings";

        public const string SslSettingsName = "Microsoft.WebServer.SslSettings";
        public static readonly string PATH = $"{WebServer.Defines.PATH}/{ENDPOINT}";
        public static readonly ResDef Resource = new ResDef("ssl", new Guid("4FFCD2E3-884B-4CEC-8FD1-4E0AF02401B3"), ENDPOINT);
    }
}
