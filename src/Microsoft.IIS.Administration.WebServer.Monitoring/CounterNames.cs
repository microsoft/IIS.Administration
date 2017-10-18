// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using Microsoft.IIS.Administration.Monitoring;

namespace Microsoft.IIS.Administration.WebServer.Monitoring
{
    class ProcessCounterNames
    {
        private const string _enCategory = "Process";
        private const string _enPercentCpu = "% Processor Time";
        private const string _enPrivateWorkingSet = "Working Set - Private";
        private const string _enWorkingSet = "Working Set";
        private const string _enVirtualBytes = "Virtual Bytes";
        private const string _enPrivateBytes = "Private Bytes";
        private const string _enThreadCount = "Thread Count";
        private const string _enIOReadSec = "IO Read Operations/sec";
        private const string _enIOWriteSec = "IO Write Operations/sec";
        private const string _enProcessId = "ID Process";
        private const string _enHandleCount = "Handle Count";
        private const string _enPageFaultsSec = "Page Faults/sec";

        private static string _category = null;
        private static string _percentCpu = null;
        private static string _privateWorkingSet = null;
        private static string _workingSet = null;
        private static string _virtualBytes = null;
        private static string _privateBytes = null;
        private static string _threadCount = null;
        private static string _ioReadSec = null;
        private static string _ioWriteSec = null;
        private static string _processId = null;
        private static string _handleCount = null;
        private static string _pageFaultsSec = null;

        private static string[] _counterNames = null;

        public string Category { get { return _category; } }

        public string PercentCpu { get { return _percentCpu; } }

        public string PrivateWorkingSet { get { return _privateWorkingSet; } }

        public string WorkingSet { get { return _workingSet; } }

        public string VirtualBytes { get { return _virtualBytes; } }

        public string PrivateBytes { get { return _privateBytes; } }

        public string ThreadCount { get { return _threadCount; } }

        public string IOReadSec { get { return _ioReadSec; } }

        public string IOWriteSec { get { return _ioWriteSec; } }

        public string ProcessId { get { return _processId; } }

        public string HandleCount { get { return _handleCount; } }

        public string PageFaultsSec { get { return _pageFaultsSec; } }

        public string[] CounterNames { get { return _counterNames; } }

        public ProcessCounterNames(ICounterTranslator translator)
        {
            if (_counterNames == null) {
                string category = translator.TranslateCategory(_enCategory);
                string percentCpu = translator.TranslateCounterName(_enCategory, _enPercentCpu);
                string privateWorkingSet = translator.TranslateCounterName(_enCategory, _enPrivateWorkingSet);
                string workingSet = translator.TranslateCounterName(_enCategory, _enWorkingSet);
                string virtualBytes = translator.TranslateCounterName(_enCategory, _enVirtualBytes);
                string privateBytes = translator.TranslateCounterName(_enCategory, _enPrivateBytes);
                string threadCount = translator.TranslateCounterName(_enCategory, _enThreadCount);
                string ioReadSec = translator.TranslateCounterName(_enCategory, _enIOReadSec);
                string ioWriteSec = translator.TranslateCounterName(_enCategory, _enIOWriteSec);
                string processId = translator.TranslateCounterName(_enCategory, _enProcessId);
                string handleCount = translator.TranslateCounterName(_enCategory, _enHandleCount);
                string pageFaultsSec = translator.TranslateCounterName(_enCategory, _enPageFaultsSec);

                var names = new string[] {
                    percentCpu,
                    privateWorkingSet,
                    workingSet,
                    virtualBytes,
                    privateBytes,
                    threadCount,
                    ioReadSec,
                    ioWriteSec,
                    processId,
                    handleCount,
                    pageFaultsSec
                };

                _category = category;
                _percentCpu = percentCpu;
                _privateWorkingSet = privateWorkingSet;
                _workingSet = workingSet;
                _virtualBytes = virtualBytes;
                _privateBytes = privateBytes;
                _threadCount = threadCount;
                _ioReadSec = ioReadSec;
                _ioWriteSec = ioWriteSec;
                _processId = processId;
                _handleCount = handleCount;
                _pageFaultsSec = pageFaultsSec;

                _counterNames = names;
            }
        }
    }

    class WorkerProcessCounterNames
    {
        private const string _enCategory = "W3SVC_W3WP";
        private const string _enActiveRequests = "Active Requests";
        private const string _enRequestsSec = "Requests / Sec";
        private const string _enTotalRequests = "Total HTTP Requests Served";
        private const string _enCurrentFileCacheMemoryUsage = "Current File Cache Memory Usage";
        private const string _enCurrentFilesCached = "Current Files Cached";
        private const string _enCurrentUrisCached = "Current URIs Cached";
        private const string _enFileCacheHits = "File Cache Hits";
        private const string _enFileCacheMisses = "File Cache Misses";
        private const string _enOutputCacheCurrentItems = "Output Cache Current Items";
        private const string _enOutputCacheCurrentMemoryUsage = "Output Cache Current Memory Usage";
        private const string _enOutputCacheTotalHits = "Output Cache Total Hits";
        private const string _enOutputCacheTotalMisses = "Output Cache Total Misses";
        private const string _enTotalFilesCached = "Total Files Cached";
        private const string _enTotalUrisCached = "Total URIs Cached";
        private const string _enUriCacheHits = "URI Cache Hits";
        private const string _enUriCacheMisses = "URI Cache Misses";

        private static string _category = null;
        private static string _activeRequests = null;
        private static string _requestsSec = null;
        private static string _totalRequests = null;
        private static string _currentFileCacheMemoryUsage = null;
        private static string _currentFilesCached = null;
        private static string _currentUrisCached = null;
        private static string _fileCacheHits = null;
        private static string _fileCacheMisses = null;
        private static string _outputCacheCurrentItems = null;
        private static string _outputCacheCurrentMemoryUsage = null;
        private static string _outputCacheTotalHits = null;
        private static string _outputCacheTotalMisses = null;
        private static string _totalFilesCached = null;
        private static string _totalUrisCached = null;
        private static string _uriCacheHits = null;
        private static string _uriCacheMisses = null;
        private static string[] _counterNames = null;

        public string Category { get { return _category; } }

        public string ActiveRequests { get { return _activeRequests; } }

        public string RequestsSec { get { return _requestsSec; } }

        public string TotalRequests { get { return _totalRequests; } }

        public string CurrentFileCacheMemoryUsage { get { return _currentFileCacheMemoryUsage; } }

        public string CurrentFilesCached { get { return _currentFilesCached; } }

        public string CurrentUrisCached { get { return _currentUrisCached; } }

        public string FileCacheHits { get { return _fileCacheHits; } }

        public string FileCacheMisses { get { return _fileCacheMisses; } }

        public string OutputCacheCurrentItems { get { return _outputCacheCurrentItems; } }

        public string OutputCacheCurrentMemoryUsage { get { return _outputCacheCurrentMemoryUsage; } }

        public string OutputCacheTotalHits { get { return _outputCacheTotalHits; } }

        public string OutputCacheTotalMisses { get { return _outputCacheTotalMisses; } }

        public string TotalFilesCached { get { return _totalFilesCached; } }

        public string TotalUrisCached { get { return _totalUrisCached; } }

        public string UriCacheHits { get { return _uriCacheHits; } }

        public string UriCacheMisses { get { return _uriCacheMisses; } }

        public string[] CounterNames { get { return _counterNames; } }

        public WorkerProcessCounterNames(ICounterTranslator translator)
        {
            if (_counterNames == null) {
                string category = translator.TranslateCategory(_enCategory);
                string activeRequests = translator.TranslateCounterName(_enCategory, _enActiveRequests);
                string requestsSec = translator.TranslateCounterName(_enCategory, _enRequestsSec);
                string totalRequests = translator.TranslateCounterName(_enCategory, _enTotalRequests);
                string currentFileCacheMemoryUsage = translator.TranslateCounterName(_enCategory, _enCurrentFileCacheMemoryUsage);
                string currentFilesCached = translator.TranslateCounterName(_enCategory, _enCurrentFilesCached);
                string currentUrisCached = translator.TranslateCounterName(_enCategory, _enCurrentUrisCached);
                string fileCacheHits = translator.TranslateCounterName(_enCategory, _enFileCacheHits);
                string fileCacheMisses = translator.TranslateCounterName(_enCategory, _enFileCacheMisses);
                string outputCacheCurrentItems = translator.TranslateCounterName(_enCategory, _enOutputCacheCurrentItems);
                string outputCacheCurrentMemoryUsage = translator.TranslateCounterName(_enCategory, _enOutputCacheCurrentMemoryUsage);
                string outputCacheTotalHits = translator.TranslateCounterName(_enCategory, _enOutputCacheTotalHits);
                string outputCacheTotalMisses = translator.TranslateCounterName(_enCategory, _enOutputCacheTotalMisses);
                string totalFilesCached = translator.TranslateCounterName(_enCategory, _enTotalFilesCached);
                string totalUrisCached = translator.TranslateCounterName(_enCategory, _enTotalUrisCached);
                string uriCacheHits = translator.TranslateCounterName(_enCategory, _enUriCacheHits);
                string uriCacheMisses = translator.TranslateCounterName(_enCategory, _enUriCacheMisses);

                var names = new string[] {
                    activeRequests,
                    requestsSec,
                    totalRequests,
                    currentFileCacheMemoryUsage,
                    currentFilesCached,
                    currentUrisCached,
                    fileCacheHits,
                    fileCacheMisses,
                    outputCacheCurrentItems,
                    outputCacheCurrentMemoryUsage,
                    outputCacheTotalHits,
                    outputCacheTotalMisses,
                    totalFilesCached,
                    totalUrisCached,
                    uriCacheHits,
                    uriCacheMisses
                };

                _category = category;
                _activeRequests = activeRequests;
                _requestsSec = requestsSec;
                _totalRequests = totalRequests;
                _currentFileCacheMemoryUsage = currentFileCacheMemoryUsage;
                _currentFilesCached = currentFilesCached;
                _currentUrisCached = currentUrisCached;
                _fileCacheHits = fileCacheHits;
                _fileCacheMisses = fileCacheMisses;
                _outputCacheCurrentItems = outputCacheCurrentItems;
                _outputCacheCurrentMemoryUsage = outputCacheCurrentMemoryUsage;
                _outputCacheTotalHits = outputCacheTotalHits;
                _outputCacheTotalMisses = outputCacheTotalMisses;
                _totalFilesCached = totalFilesCached;
                _totalUrisCached = totalUrisCached;
                _uriCacheHits = uriCacheHits;
                _uriCacheMisses = uriCacheMisses;

                _counterNames = names;
            }
        }

        public static string GetInstanceName(int processId, string appPoolName)
        {
            return processId + "_" + appPoolName;
        }
    }

    class WebSiteCounterNames
    {
        private const string _enCategory = "Web Service";
        private const string _enServiceUptime = "Service Uptime";
        private const string _enBytesRecvSec = "Bytes Received/sec";
        private const string _enBytesSentSec = "Bytes Sent/sec";
        private const string _enTotalBytesSent = "Total Bytes Sent";
        private const string _enTotalBytesRecv = "Total Bytes Received";
        private const string _enConnectionAttemptsSec = "Connection Attempts/sec";
        private const string _enCurrentConnections = "Current Connections";
        private const string _enTotalConnectionAttempts = "Total Connection Attempts (all instances)";
        private const string _enTotalMethodRequestsSec = "Total Method Requests/sec";
        private const string _enTotalOtherMethodRequestsSec = "Other Request Methods/sec";
        private const string _enTotalMethodRequests = "Total Method Requests";
        private const string _enTotalOtherMethodRequests = "Total Other Request Methods";

        private static string _category = null;
        private static string _serviceUptime = null;
        private static string _bytesRecvSec = null;
        private static string _bytesSentSec = null;
        private static string _totalBytesSent = null;
        private static string _totalBytesRecv = null;
        private static string _connectionAttemptsSec = null;
        private static string _currentConnections = null;
        private static string _totalConnectionAttempts = null;
        private static string _totalMethodRequestsSec = null;
        private static string _totalOtherMethodRequestsSec = null;
        private static string _totalMethodRequests = null;
        private static string _totalOtherMethodRequests = null;
        private static string[] _counterNames = null;

        public string Category { get { return _category; } }

        public string ServiceUptime { get { return _serviceUptime; } }

        public string BytesRecvSec { get { return _bytesRecvSec; } }

        public string BytesSentSec { get { return _bytesSentSec; } }

        public string TotalBytesSent { get { return _totalBytesSent; } }

        public string TotalBytesRecv { get { return _totalBytesRecv; } }

        public string ConnectionAttemptsSec { get { return _connectionAttemptsSec; } }

        public string CurrentConnections { get { return _currentConnections; } }

        public string TotalConnectionAttempts { get { return _totalConnectionAttempts; } }

        public string TotalMethodRequestsSec { get { return _totalMethodRequestsSec; } }

        public string TotalOtherMethodRequestsSec { get { return _totalOtherMethodRequestsSec; } }

        public string TotalMethodRequests { get { return _totalMethodRequests; } }

        public string TotalOtherMethodRequests { get { return _totalOtherMethodRequests; } }

        public string[] CounterNames { get { return _counterNames; } }

        public WebSiteCounterNames(ICounterTranslator translator)
        {
            if (_counterNames == null) {
                string category = translator.TranslateCategory(_enCategory);
                string serviceUptime = translator.TranslateCounterName(_enCategory, _enServiceUptime);
                string bytesRecvSec = translator.TranslateCounterName(_enCategory, _enBytesRecvSec);
                string bytesSentSec = translator.TranslateCounterName(_enCategory, _enBytesSentSec);
                string totalBytesSent = translator.TranslateCounterName(_enCategory, _enTotalBytesSent);
                string totalBytesRecv = translator.TranslateCounterName(_enCategory, _enTotalBytesRecv);
                string connectionAttemptsSec = translator.TranslateCounterName(_enCategory, _enConnectionAttemptsSec);
                string currentConnections = translator.TranslateCounterName(_enCategory, _enCurrentConnections);
                string totalConnectionAttempts = translator.TranslateCounterName(_enCategory, _enTotalConnectionAttempts);
                string totalMethodRequestsSec = translator.TranslateCounterName(_enCategory, _enTotalMethodRequestsSec);
                string totalOtherMethodRequestsSec = translator.TranslateCounterName(_enCategory, _enTotalOtherMethodRequestsSec);
                string totalMethodRequests = translator.TranslateCounterName(_enCategory, _enTotalMethodRequests);
                string totalOtherMethodRequests = translator.TranslateCounterName(_enCategory, _enTotalOtherMethodRequests);

                var names = new string[] {
                    serviceUptime,
                    bytesRecvSec,
                    bytesSentSec,
                    totalBytesSent,
                    totalBytesRecv,
                    connectionAttemptsSec,
                    currentConnections,
                    totalConnectionAttempts,
                    totalMethodRequestsSec,
                    totalOtherMethodRequestsSec,
                    totalMethodRequests,
                    totalOtherMethodRequests
                };

                _category = category;
                _serviceUptime = serviceUptime;
                _bytesRecvSec = bytesRecvSec;
                _bytesSentSec = bytesSentSec;
                _totalBytesSent = totalBytesSent;
                _totalBytesRecv = totalBytesRecv;
                _connectionAttemptsSec = connectionAttemptsSec;
                _currentConnections = currentConnections;
                _totalConnectionAttempts = totalConnectionAttempts;
                _totalMethodRequestsSec = totalMethodRequestsSec;
                _totalOtherMethodRequestsSec = totalOtherMethodRequestsSec;
                _totalMethodRequests = totalMethodRequests;
                _totalOtherMethodRequests = totalOtherMethodRequests;

                _counterNames = names;
            }
        }
    }

    class MemoryCounterNames
    {
        private const string _enCategory = "Memory";
        private const string _enAvailableBytes = "Available Bytes";

        private static string _category = null;
        private static string _availableBytes = null;
        private static string[] _counterNames = null;

        public string Category { get { return _category; } }

        public string AvailableBytes { get { return _availableBytes; } }

        public string[] CounterNames { get { return _counterNames; } }

        public MemoryCounterNames(ICounterTranslator translator)
        {
            if (_counterNames == null) {
                string category = translator.TranslateCategory(_enCategory);
                string availableBytes = translator.TranslateCounterName(_enCategory, _enAvailableBytes);

                string[] names = new string[] {
                    availableBytes
                };

                _category = category;
                _availableBytes = availableBytes;

                _counterNames = names;
            }
        }
    }

    class CacheCounterNames
    {
        private const string _enCategory = "Web Service Cache";
        private const string _enCurrentFileCacheMemoryUsage = "Current File Cache Memory Usage";
        private const string _enCurrentFilesCached = "Current Files Cached";
        private const string _enCurrentUrisCached = "Current URIs Cached";
        private const string _enFileCacheHits = "File Cache Hits";
        private const string _enFileCacheMisses = "File Cache Misses";
        private const string _enOutputCacheCurrentItems = "Output Cache Current Items";
        private const string _enOutputCacheCurrentMemoryUsage = "Output Cache Current Memory Usage";
        private const string _enOutputCacheTotalHits = "Output Cache Total Hits";
        private const string _enOutputCacheTotalMisses = "Output Cache Total Misses";
        private const string _enTotalFilesCached = "Total Files Cached";
        private const string _enTotalUrisCached = "Total URIs Cached";
        private const string _enUriCacheHits = "URI Cache Hits";
        private const string _enUriCacheMisses = "URI Cache Misses";

        private static string _category = null;
        private static string _currentFileCacheMemoryUsage = null;
        private static string _currentFilesCached = null;
        private static string _currentUrisCached = null;
        private static string _fileCacheHits = null;
        private static string _fileCacheMisses = null;
        private static string _outputCacheCurrentItems = null;
        private static string _outputCacheCurrentMemoryUsage = null;
        private static string _outputCacheTotalHits = null;
        private static string _outputCacheTotalMisses = null;
        private static string _totalFilesCached = null;
        private static string _totalUrisCached = null;
        private static string _uriCacheHits = null;
        private static string _uriCacheMisses = null;
        private static string[] _counterNames = null;

        public string Category { get { return _category; } }

        public string CurrentFileCacheMemoryUsage { get { return _currentFileCacheMemoryUsage; } }

        public string CurrentFilesCached { get { return _currentFilesCached; } }

        public string CurrentUrisCached { get { return _currentUrisCached; } }

        public string FileCacheHits { get { return _fileCacheHits; } }

        public string FileCacheMisses { get { return _fileCacheMisses; } }

        public string OutputCacheCurrentItems { get { return _outputCacheCurrentItems; } }

        public string OutputCacheCurrentMemoryUsage { get { return _outputCacheCurrentMemoryUsage; } }

        public string OutputCacheTotalHits { get { return _outputCacheTotalHits; } }

        public string OutputCacheTotalMisses { get { return _outputCacheTotalMisses; } }

        public string TotalFilesCached { get { return _totalFilesCached; } }

        public string TotalUrisCached { get { return _totalUrisCached; } }

        public string UriCacheHits { get { return _uriCacheHits; } }

        public string UriCacheMisses { get { return _uriCacheMisses; } }

        public string[] CounterNames { get { return _counterNames; } }

        public CacheCounterNames(ICounterTranslator translator)
        {
            if (_counterNames == null) {
                string category = translator.TranslateCategory(_enCategory);
                string currentFileCacheMemoryUsage = translator.TranslateCounterName(_enCategory, _enCurrentFileCacheMemoryUsage);
                string currentFilesCached = translator.TranslateCounterName(_enCategory, _enCurrentFilesCached);
                string currentUrisCached = translator.TranslateCounterName(_enCategory, _enCurrentUrisCached);
                string fileCacheHits = translator.TranslateCounterName(_enCategory, _enFileCacheHits);
                string fileCacheMisses = translator.TranslateCounterName(_enCategory, _enFileCacheMisses);
                string outputCacheCurrentItems = translator.TranslateCounterName(_enCategory, _enOutputCacheCurrentItems);
                string outputCacheCurrentMemoryUsage = translator.TranslateCounterName(_enCategory, _enOutputCacheCurrentMemoryUsage);
                string outputCacheTotalHits = translator.TranslateCounterName(_enCategory, _enOutputCacheTotalHits);
                string outputCacheTotalMisses = translator.TranslateCounterName(_enCategory, _enOutputCacheTotalMisses);
                string totalFilesCached = translator.TranslateCounterName(_enCategory, _enTotalFilesCached);
                string totalUrisCached = translator.TranslateCounterName(_enCategory, _enTotalUrisCached);
                string uriCacheHits = translator.TranslateCounterName(_enCategory, _enUriCacheHits);
                string uriCacheMisses = translator.TranslateCounterName(_enCategory, _enUriCacheMisses);

                var names = new string[] {
                    currentFileCacheMemoryUsage,
                    currentFilesCached,
                    currentUrisCached,
                    fileCacheHits,
                    fileCacheMisses,
                    outputCacheCurrentItems,
                    outputCacheCurrentMemoryUsage,
                    outputCacheTotalHits,
                    outputCacheTotalMisses,
                    totalFilesCached,
                    totalUrisCached,
                    uriCacheHits,
                    uriCacheMisses
                };

                _category = category;
                _currentFileCacheMemoryUsage = currentFileCacheMemoryUsage;
                _currentFilesCached = currentFilesCached;
                _currentUrisCached = currentUrisCached;
                _fileCacheHits = fileCacheHits;
                _fileCacheMisses = fileCacheMisses;
                _outputCacheCurrentItems = outputCacheCurrentItems;
                _outputCacheCurrentMemoryUsage = outputCacheCurrentMemoryUsage;
                _outputCacheTotalHits = outputCacheTotalHits;
                _outputCacheTotalMisses = outputCacheTotalMisses;
                _totalFilesCached = totalFilesCached;
                _totalUrisCached = totalUrisCached;
                _uriCacheHits = uriCacheHits;
                _uriCacheMisses = uriCacheMisses;

                _counterNames = names;
            }
        }
    }

    class ProcessorCounterNames
    {
        private const string _enCategory = "Processor";
        private const string _enIdleTime = "% Idle Time";

        private static string _category = "Processor";
        private static string _idleTime = "% Idle Time";
        private static string[] _counterNames = null;

        public string Category { get { return _category; } }

        public string IdleTime { get { return _idleTime; } }

        public string[] CounterNames { get { return _counterNames; } }

        public ProcessorCounterNames(ICounterTranslator translator)
        {
            if (_counterNames == null) {
                string category = translator.TranslateCategory(_enCategory);
                string idleTime = translator.TranslateCounterName(_enCategory, _enIdleTime);

                var names = new string[] {
                    idleTime
                };

                _category = category;
                _idleTime = idleTime;

                _counterNames = names;
            }
        }
    }
}
