// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.WorkerProcesses {
    using Core;
    using System;


    public class Defines
    {
        //
        // WorkerProcess
        private const string ENDPOINT = "worker-processes";

        public const string WorkerProcessesName = "Microsoft.WebServer.WorkerProcesses";
        public const string WorkerProcessName = "Microsoft.WebServer.WorkerProcess";
        public static readonly string PATH = $"{WebServer.Defines.PATH}/{ENDPOINT}";
        public static readonly ResDef Resource = new ResDef("worker_processes", new Guid("F6378A0F-3B8D-4C1B-85D8-1AB8A044A94B"), ENDPOINT);

        internal const string IDENTIFIER = "wp.id";

        //
        // WebApps
        private const string APPS_ENDPOINT = "webapps";

        internal static readonly string APPS_PATH = $"{PATH}/{APPS_ENDPOINT}";
        internal static readonly ResDef AppsResource = new ResDef("webapps", new Guid("760AAAC6-0817-489A-AF56-1D5B93227919"), APPS_ENDPOINT);

        //
        // Sites
        private const string SITES_ENDPOINT = "websites";

        internal static readonly string SITES_PATH = $"{PATH}/{SITES_ENDPOINT}";
        internal static readonly ResDef SitesResource = new ResDef("websites", new Guid("328E223C-98C5-4F10-8107-38957D4DD306"), SITES_ENDPOINT);
    }
}
