// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestMonitor
{
    using System;

    public class RequestId {
        private const string PURPOSE = "WebServer.WorkerProcesses.Request";
        private const char DELIMITER = '\n';

        public string Id { get; private set; }
        public int ProcessId { get; private set; }

        public string Uuid { get; private set; }

        private RequestId() { }
        

        public RequestId(string uuid) {
            if (string.IsNullOrEmpty(uuid)) {
                throw new ArgumentNullException(nameof(uuid));
            }

            var data = Core.Utils.Uuid.Decode(uuid, PURPOSE).Split(DELIMITER);

            ProcessId = int.Parse(data[0]);
            Id = data[1];
            Uuid = uuid;
        }

        public RequestId(int processId, string requestId) {
            if (processId <= 0) {
                throw new ArgumentException(nameof(processId));
            }

            if (string.IsNullOrEmpty(requestId)) {
                throw new ArgumentNullException(nameof(requestId));
            }

            ProcessId = processId;
            Id = requestId;
            Uuid = Core.Utils.Uuid.Encode($"{processId}{DELIMITER}{requestId}", PURPOSE);
        }
    }
}
