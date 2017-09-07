// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Monitoring
{
    class WebSiteSnapshot : IWebSiteSnapshot
    {
        public string Name { get; set; }

        public long Uptime { get; set; }

        public long BytesRecvSec { get; set; }

        public long BytesSentSec { get; set; }

        public long TotalConnectionAttempts { get; set; }

        public long ConnectionAttemptsSec { get; set; }

        public long CurrentConnections { get; set; }

        public long TotalRequestsSec { get; set; }

        public long TotalRequests { get; set; }
    }
}
