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
    }
}
