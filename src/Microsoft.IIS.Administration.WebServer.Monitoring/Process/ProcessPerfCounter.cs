// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Monitoring
{
    using Microsoft.IIS.Administration.Monitoring;

    class ProcessPerfCounter : PerfCounter
    {
        public ProcessPerfCounter(string name, string instanceName, string categoryName, int processId) : base(name, instanceName, categoryName)
        {
            ProcessId = processId;
        }

        public int ProcessId { get; set; }
    }
}
