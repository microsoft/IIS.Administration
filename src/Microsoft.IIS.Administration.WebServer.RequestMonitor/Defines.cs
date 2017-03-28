// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestMonitor {
    using Core;
    using System;

    public class Defines {
        internal const string WP_IDENTIFIER = "wp.id";

        private const string ENDPOINT = "http-request-monitor";
        private const string REQUESTS_ENDPOINT = "requests";

        //
        // Request Monitoring
        public const string MonitorName = "Microsoft.WebServer.RequestMonitoring";
        public static readonly string PATH = $"{WebServer.Defines.PATH}/{ENDPOINT}";
        public static readonly ResDef Resource = new ResDef("request_monitor", new Guid("FE279A67-AE11-4679-AA6A-483362E35B2C"), ENDPOINT);

        //
        // Requests
        public const string RequestsName = "Microsoft.WebServer.RequestMonitoring.Requests";
        public const string RequestName = "Microsoft.WebServer.RequestMonitoring.Request";
        public static readonly string REQUESTS_PATH = $"{PATH}/{REQUESTS_ENDPOINT}";
        public static readonly ResDef RequestsResource = new ResDef("requests", new Guid("8EE648E4-7098-4926-BAAC-DF2CAB2E170E"), REQUESTS_ENDPOINT);
    }
}
