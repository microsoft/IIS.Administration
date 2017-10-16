// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Monitoring
{
    static class ProcessCounterNames
    {
        public const string Category = "Process";
        public const string PercentCpu = "% Processor Time";
        public const string PrivateWorkingSet = "Working Set - Private";
        public const string WorkingSet = "Working Set";
        public const string VirtualBytes = "Virtual Bytes";
        public const string PrivateBytes = "Private Bytes";
        public const string ThreadCount = "Thread Count";
        public const string IOReadSec = "IO Read Operations/sec";
        public const string IOWriteSec = "IO Write Operations/sec";
        public const string ProcessId = "Id Process";
        public const string HandleCount = "Handle Count";
        public const string PageFaultsSec = "Page Faults/sec";

        public static readonly string[] CounterNames = new string[] {
            PercentCpu,
            PrivateWorkingSet,
            WorkingSet,
            VirtualBytes,
            PrivateBytes,
            ThreadCount,
            IOReadSec,
            IOWriteSec,
            ProcessId,
            HandleCount,
            PageFaultsSec
        };
    }

    static class WorkerProcessCounterNames
    {
        public const string Category = "W3SVC_W3WP";
        public const string ActiveRequests = "Active Requests";
        public const string RequestsSec = "Requests / Sec";
        public const string TotalRequests = "Total HTTP Requests Served";
        public const string CurrentFileCacheMemoryUsage = "Current File Cache Memory Usage";
        public const string CurrentFilesCached = "Current Files Cached";
        public const string CurrentUrisCached = "Current URIs Cached";
        public const string FileCacheHits = "File Cache Hits";
        public const string FileCacheMisses = "File Cache Misses";
        public const string OutputCacheCurrentItems = "Output Cache Current Items";
        public const string OutputCacheCurrentMemoryUsage = "Output Cache Current Memory Usage";
        public const string OutputCacheTotalHits = "Output Cache Total Hits";
        public const string OutputCacheTotalMisses = "Output Cache Total Misses";
        public const string TotalFilesCached = "Total Files Cached";
        public const string TotalUrisCached = "Total URIs Cached";
        public const string UriCacheHits = "URI Cache Hits";
        public const string UriCacheMisses = "URI Cache Misses";

        public static readonly string[] CounterNames = new string[] {
            ActiveRequests,
            RequestsSec,
            TotalRequests,
            CurrentFileCacheMemoryUsage,
            CurrentFilesCached,
            CurrentUrisCached,
            FileCacheHits,
            FileCacheMisses,
            OutputCacheCurrentItems,
            OutputCacheCurrentMemoryUsage,
            OutputCacheTotalHits,
            OutputCacheTotalMisses,
            TotalFilesCached,
            TotalUrisCached,
            UriCacheHits,
            UriCacheMisses
        };

        public static string GetInstanceName(int processId, string appPoolName)
        {
            return processId + "_" + appPoolName;
        }
    }

    static class WebSiteCounterNames
    {
        public const string Category = "Web Service";
        public const string ServiceUptime = "Service Uptime";
        public const string BytesRecvSec = "Bytes Received/sec";
        public const string BytesSentSec = "Bytes Sent/sec";
        public const string TotalBytesSent = "Total Bytes Sent";
        public const string TotalBytesRecv = "Total Bytes Received";
        public const string ConnectionAttemptsSec = "Connection Attempts/sec";
        public const string CurrentConnections = "Current Connections";
        public const string TotalConnectionAttempts = "Total Connection Attempts (all instances)";
        public const string TotalMethodRequestsSec = "Total Method Requests/sec";
        public const string TotalOtherMethodRequestsSec = "Other Request Methods/sec";
        public const string TotalMethodRequests = "Total Method Requests";
        public const string TotalOtherMethodRequests = "Total Other Request Methods";

        public static readonly string[] CounterNames = new string[] {
            ServiceUptime,
            BytesRecvSec,
            BytesSentSec,
            TotalBytesRecv,
            TotalBytesSent,
            ConnectionAttemptsSec,
            CurrentConnections,
            TotalConnectionAttempts,
            TotalMethodRequestsSec,
            TotalOtherMethodRequestsSec,
            TotalMethodRequests,
            TotalOtherMethodRequests
        };
    }

    static class MemoryCounterNames
    {
        public const string Category = "Memory";
        public const string AvailableBytes = "Available Bytes";

        public static readonly string[] CounterNames = new string[] {
            AvailableBytes
        };
    }

    static class CacheCounterNames
    {
        public const string Category = "Web Service Cache";
        public const string CurrentFileCacheMemoryUsage = "Current File Cache Memory Usage";
        public const string CurrentFilesCached = "Current Files Cached";
        public const string CurrentUrisCached = "Current URIs Cached";
        public const string FileCacheHits = "File Cache Hits";
        public const string FileCacheMisses = "File Cache Misses";
        public const string OutputCacheCurrentItems = "Output Cache Current Items";
        public const string OutputCacheCurrentMemoryUsage = "Output Cache Current Memory Usage";
        public const string OutputCacheTotalHits = "Output Cache Total Hits";
        public const string OutputCacheTotalMisses = "Output Cache Total Misses";
        public const string TotalFilesCached = "Total Files Cached";
        public const string TotalUrisCached = "Total URIs Cached";
        public const string UriCacheHits = "URI Cache Hits";
        public const string UriCacheMisses = "URI Cache Misses";

        public static readonly string[] CounterNames = new string[] {
            CurrentFileCacheMemoryUsage,
            CurrentFilesCached,
            CurrentUrisCached,
            FileCacheHits,
            FileCacheMisses,
            OutputCacheCurrentItems,
            OutputCacheCurrentMemoryUsage,
            OutputCacheTotalHits,
            OutputCacheTotalMisses,
            TotalFilesCached,
            TotalUrisCached,
            UriCacheHits,
            UriCacheMisses
        };
    }

    static class ProcessorCounterNames
    {
        public const string Category = "Processor";
        public const string IdleTime = "% Idle Time";

        public static readonly string[] CounterNames = new string[] {
            IdleTime
        };
    }
}
