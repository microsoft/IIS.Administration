// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.WorkerProcesses
{
    using System;

    public class WorkerProcessId
    {
        private const char DELIMITER = '\n';
        private const string PURPOSE = "WebServer.WorkerProcesses";

        public int Id { get; private set; }

        public string Uuid { get; private set; }

        private WorkerProcessId() { }
        
        public WorkerProcessId(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) {
                throw new ArgumentNullException(uuid);
            }

            var info = Core.Utils.Uuid.Decode(uuid, PURPOSE).Split(DELIMITER);

            this.Id = int.Parse(info[0]);
            this.Uuid = uuid;
        }

        public WorkerProcessId(int id, Guid guid) {
            this.Id = id;
            this.Uuid = Core.Utils.Uuid.Encode($"{this.Id}{DELIMITER}{guid.GetHashCode()}", PURPOSE);
        }
    }
}
