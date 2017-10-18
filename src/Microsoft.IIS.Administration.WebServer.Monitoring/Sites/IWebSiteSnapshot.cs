// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Monitoring
{
    public interface IWebSiteSnapshot
    {
        string Name { get; }

        long Uptime { get; }

        long BytesRecvSec { get; set; }

        long BytesSentSec { get; set; }

        long TotalBytesSent { get; set; }

        long TotalBytesRecv { get; set; }

        long TotalConnectionAttempts { get; set; }

        long ConnectionAttemptsSec { get; set; }

        long CurrentConnections { get; set; }

        long RequestsSec { get; set; }

        long TotalRequests { get; set; }

        long ActiveRequests { get; set; }

        long FileCacheMemoryUsage { get; set; }

        long CurrentFilesCached { get; set; }

        long CurrentUrisCached { get; set; }

        long FileCacheHits { get; set; }

        long FileCacheMisses { get; set; }

        long OutputCacheCurrentItems { get; set; }

        long OutputCacheCurrentMemoryUsage { get; set; }

        long OutputCacheTotalHits { get; set; }

        long OutputCacheTotalMisses { get; set; }

        long TotalFilesCached { get; set; }

        long TotalUrisCached { get; set; }

        long UriCacheHits { get; set; }

        long UriCacheMisses { get; set; }

        long PageFaultsSec { get; set; }

        long IOWriteSec { get; set; }

        long IOReadSec { get; set; }

        long WorkingSet { get; set; }

        long PrivateWorkingSet { get; set; }

        long ThreadCount { get; set; }

        long PrivateBytes { get; set; }

        long AvailableMemory { get; set; }

        long SystemMemoryInUse { get; set; }

        long TotalInstalledMemory { get; set; }

        long HandleCount { get; set; }

        long ProcessCount { get; set; }

        long PercentCpuTime { get; set; }
    }
}
