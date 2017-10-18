// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Monitoring
{
    public interface IWebServerSnapshot
    {
        long BytesSentSec { get; }

        long BytesRecvSec { get; }

        long TotalBytesSent { get; set; }

        long TotalBytesRecv { get; set; }

        long TotalConnectionAttempts { get; set; }

        long ConnectionAttemptsSec { get; set; }

        long CurrentConnections { get; set; }

        long ActiveRequests { get; }

        long RequestsSec { get; }

        long TotalRequests { get; }

        long PrivateBytes { get; }

        long PercentCpuTime { get; }

        long SystemPercentCpuTime { get; set; }

        long PrivateWorkingSet { get; set; }

        long WorkingSet { get; }

        long ThreadCount { get; }

        long HandleCount { get; set; }

        long ProcessCount { get; }

        long IOReadSec { get; set; }

        long IOWriteSec { get; set; }

        long FileCacheHits { get; set; }

        long FileCacheMisses { get; set; }

        long AvailableMemory { get; set; }

        long SystemMemoryInUse { get; set; }

        long TotalInstalledMemory { get; set; }

        long FileCacheMemoryUsage { get; set; }

        long UriCacheMisses { get; set; }

        long UriCacheHits { get; set; }

        long TotalUrisCached { get; set; }

        long TotalFilesCached { get; set; }

        long OutputCacheMemoryUsage { get; set; }

        long OutputCacheCurrentItems { get; set; }

        long OutputCacheTotalHits { get; set; }

        long OutputCacheTotalMisses { get; set; }

        long CurrentUrisCached { get; set; }

        long CurrentFilesCached { get; set; }

        long PageFaultsSec { get; set; }
    }
}
