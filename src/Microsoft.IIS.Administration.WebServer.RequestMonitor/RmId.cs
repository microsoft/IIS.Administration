// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestMonitor
{
    using System;

    public class RmId
    {
        private const string PURPOSE = "WebServer.RequestMonitoring";

        public string Uuid { get; private set; }

        public RmId()
        {
            Uuid = Core.Utils.Uuid.Encode($"", PURPOSE);
        }

        public RmId(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) {
                throw new ArgumentNullException(nameof(uuid));
            }

            Uuid = uuid;
        }
    }
}
