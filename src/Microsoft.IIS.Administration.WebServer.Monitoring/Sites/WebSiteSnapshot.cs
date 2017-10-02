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

        public long ActiveRequests { get; set; }

        public long Percent500 { get; set; }

        public long RequestsSec { get; set; }

        public long FileCacheMemoryUsage { get; set; }

        public long CurrentFilesCached { get; set; }

        public long CurrentUrisCached { get; set; }

        public long FileCacheHits { get; set; }

        public long FileCacheMisses { get; set; }

        public long OutputCacheCurrentItems { get; set; }

        public long OutputCacheCurrentMemoryUsage { get; set; }

        public long OutputCacheTotalHits { get; set; }

        public long OutputCacheTotalMisses { get; set; }

        public long TotalFilesCached { get; set; }

        public long TotalUrisCached { get; set; }

        public long UriCacheHits { get; set; }

        public long UriCacheMisses { get; set; }

        public long PageFaultsSec { get; set; }

        public long IOWriteSec { get; set; }

        public long IOReadSec { get; set; }

        public long WorkingSet { get; set; }

        public long PrivateWorkingSet { get; set; }

        public long ThreadCount { get; set; }

        public long PrivateBytes { get; set; }

        public long HandleCount { get; set; }

        public long PercentCpuTime { get; set; }
    }
}
