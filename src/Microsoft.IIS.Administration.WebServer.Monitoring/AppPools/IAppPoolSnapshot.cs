// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Monitoring
{
    public interface IAppPoolSnapshot
    {
        string Name { get; set; }

        long ActiveRequests { get; set; }

        long PercentCpuTime { get; set; }

        long PageFaultsSec { get; set; }

        long HandleCount { get; set; }

        long PrivateBytes { get; set; }

        long ThreadCount { get; set; }

        long PrivateWorkingSet { get; set; }

        long AvailableMemory { get; set; }

        long SystemMemoryInUse { get; set; }

        long TotalInstalledMemory { get; set; }

        long WorkingSet { get; set; }

        long IOReadSec { get; set; }

        long IOWriteSec { get; set; }

        long ProcessCount { get; set; }

        long RequestsSec { get; set; }

        long TotalRequests { get; set; }

        long UriCacheMisses { get; set; }

        long UriCacheHits { get; set; }

        long TotalUrisCached { get; set; }

        long TotalFilesCached { get; set; }

        long OutputCacheTotalMisses { get; set; }

        long OutputCacheTotalHits { get; set; }

        long OutputCacheCurrentMemoryUsage { get; set; }

        long OutputCacheCurrentItems { get; set; }

        long FileCacheMisses { get; set; }

        long FileCacheHits { get; set; }

        long CurrentUrisCached { get; set; }

        long CurrentFilesCached { get; set; }

        long FileCacheMemoryUsage { get; set; }
    }
}
