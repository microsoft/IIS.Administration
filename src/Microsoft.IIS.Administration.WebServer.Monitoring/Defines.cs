// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Monitoring
{
    using Core;
    using System;

    public class Defines
    {
        private const string ENDPOINT = "monitoring";

        public const string WebServerMonitoringName = "Microsoft.WebServer.Monitoring";
        public static readonly string PATH = $"{WebServer.Defines.PATH}/{ENDPOINT}";
        public static readonly ResDef WebServerMonitoringResource = new ResDef("monitoring", new Guid("2D6444DA-CFA4-4D0B-9384-0D117408EEC8"), ENDPOINT);

        public const string WebSiteMonitoringName = "Microsoft.WebServer.WebSite.Monitoring";
        public static readonly string WEBSITE_MONITORING_PATH = $"{Sites.Defines.PATH}/{ENDPOINT}";
        public static readonly ResDef WebSiteMonitoringResource = new ResDef("monitoring", new Guid("A04371D4-FEEB-41E7-8846-C27441B49A62"), ENDPOINT);

        public const string AppPoolMonitoringName = "Microsoft.WebServer.AppPool.Monitoring";
        public static readonly string APP_POOL_MONITORING_PATH = $"{AppPools.Defines.PATH}/{ENDPOINT}";
        public static readonly ResDef AppPoolMonitoringResource = new ResDef("monitoring", new Guid("7404541A-B9EB-4427-A7A7-6E4C3BB871B8"), ENDPOINT);
    }
}
