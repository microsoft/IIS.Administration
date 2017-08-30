// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Monitoring
{
    using Core;
    using System;

    public class Defines
    {
        private const string ENDPOINT = "monitoring";

        public const string MonitoringName = "Microsoft.WebServer.Monitoring";
        public static readonly string PATH = $"{WebServer.Defines.PATH}/{ENDPOINT}";
        public static readonly ResDef Resource = new ResDef("monitoring", new Guid("2D6444DA-CFA4-4D0B-9384-0D117408EEC8"), ENDPOINT);
    }
}
