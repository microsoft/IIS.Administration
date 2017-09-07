// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Monitoring
{
    using Microsoft.IIS.Administration.Monitoring;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    class WebServerMonitor : IWebServerMonitor, IDisposable
    {
        public const string WorkerProcessCategory = "W3SVC_W3WP";
        public const string WebSiteCategory = "Web Service";
        private const string CacheCategory = "Web Service Cache";
        private const string MemoryCategory = "Memory";

        private IEnumerable<int> _allProcesses;
        private IEnumerable<int> _webserverProcesses;
        private IEnumerable<IPerfCounter> _webserverCounters;
        private readonly TimeSpan RefreshRate = TimeSpan.FromSeconds(1);
        private DateTime _lastCalculation;
        private CounterMonitor _counterMonitor;
        private WebServerSnapshot _snapshot = new WebServerSnapshot();
        private CounterProvider _provider = new CounterProvider();

        public void Dispose()
        {
            if (_counterMonitor != null) {
                _counterMonitor.Dispose();
                _counterMonitor = null;
            }
        }

        public async Task<IWebServerSnapshot> GetSnapshot()
        {
            await CalculateData();
            Dispose();
            return _snapshot;
        }

        private async Task Initialize()
        {
            IEnumerable<int> webserverProcessIds = ProcessUtil.GetWebserverProcessIds();

            //
            // Set up tracking for change detection
            _allProcesses = Process.GetProcesses().Select(p => p.Id);
            _webserverProcesses = webserverProcessIds.OrderBy(id => id);
            _webserverCounters = GetWebserverCounters();

            //
            // Set up counters for data calculation
            var counters = new List<IPerfCounter>();
            counters.AddRange(GetMemoryCounters());
            counters.AddRange(GetCacheCounters());
            counters.AddRange(await ProcessUtil.GetProcessCounters(webserverProcessIds));
            counters.AddRange(_webserverCounters);

            //
            // Set up counter monitor for data retrieval
            _counterMonitor = new CounterMonitor(counters);
        }

        private async Task CalculateData()
        {
            if (_counterMonitor == null) {
                await Initialize();
            }

            if ((DateTime.UtcNow - _lastCalculation > RefreshRate) && HasChanged()) {
                await Reset();
            }

            if (_counterMonitor.Counters.Count() > 0) {
                await Query();
            }

            long bytesSentSec = 0;
            long bytesRecvSec = 0;
            long connectionAttemptsSec = 0;
            long totalConnectionAttempts = 0;
            long activeRequests = 0;
            long requestsSec = 0;
            long totalRequests = 0;
            long percentCpuTime = 0;
            long pageFaultsSec = 0;
            long handleCount = 0;
            long privateBytes = 0;
            long threadCount = 0;
            long privateWorkingSet = 0;
            long workingSet = 0;
            long IOReadSec = 0;
            long IOWriteSec = 0;
            long percent500 = 0;
            long availableBytes = 0;
            long fileCacheMemoryUsage = 0;
            long currentFilesCached = 0;
            long currentUrisCached = 0;
            long fileCacheHits = 0;
            long fileCacheMisses = 0;
            long outputCacheCurrentItems = 0;
            long outputCacheCurrentMemoryUsage = 0;
            long outputCacheTotalHits = 0;
            long outputCacheTotalMisses = 0;
            long totalFilesCached = 0;
            long totalUrisCached = 0;
            long uriCacheHits = 0;
            long uriCacheMisses = 0;

            foreach (IPerfCounter counter in _counterMonitor.Counters) {
                if (counter.CategoryName.Equals(WebSiteCategory)) {
                    switch (counter.Name) {
                        case "Bytes Received/sec":
                            bytesRecvSec += counter.Value;
                            break;
                        case "Bytes Sent/sec":
                            bytesSentSec += counter.Value;
                            break;
                        case "Connection Attempts/sec":
                            connectionAttemptsSec += counter.Value;
                            break;
                        case "Total Connection Attempts (all instances)":
                            totalConnectionAttempts += counter.Value;
                            break;
                        case "Total Method Requests/sec":
                            requestsSec += counter.Value;
                            break;
                        case "Other Request Methods/sec":
                            requestsSec += counter.Value;
                            break;
                        case "Total Method Requests":
                            totalRequests += counter.Value;
                            break;
                        case "Total Other Request Methods":
                            totalRequests += counter.Value;
                            break;
                        default:
                            break;
                    }
                }

                if (counter.CategoryName.Equals(WorkerProcessCategory)) {
                    switch (counter.Name) {
                        case "Active Requests":
                            activeRequests += counter.Value;
                            break;
                        case "% 500 HTTP Response Sent":
                            percent500 += counter.Value;
                            break;
                        default:
                            break;
                    }
                }

                if (counter.CategoryName.Equals(ProcessUtil.ProcessCategory)) {
                    switch (counter.Name) {
                        case ProcessUtil.CounterPercentCpu:
                            percentCpuTime += counter.Value;
                            break;
                        case ProcessUtil.CounterHandleCount:
                            handleCount += counter.Value;
                            break;
                        case ProcessUtil.CounterPrivateBytes:
                            privateBytes += counter.Value;
                            break;
                        case ProcessUtil.CounterThreadCount:
                            threadCount += counter.Value;
                            break;
                        case ProcessUtil.CounterPrivateWorkingSet:
                            privateWorkingSet += counter.Value;
                            break;
                        case ProcessUtil.CounterWorkingSet:
                            workingSet += counter.Value;
                            break;
                        case ProcessUtil.CounterIOReadSec:
                            IOReadSec += counter.Value;
                            break;
                        case ProcessUtil.CounterIOWriteSec:
                            IOWriteSec += counter.Value;
                            break;
                        case ProcessUtil.CounterPageFaultsSec:
                            pageFaultsSec += counter.Value;
                            break;
                        default:
                            break;
                    }
                }

                if (counter.CategoryName.Equals(MemoryCategory)) {
                    switch (counter.Name) {
                        case "Available Bytes":
                            availableBytes += counter.Value;
                            break;
                        default:
                            break;
                    }
                }

                if (counter.CategoryName.Equals(CacheCategory)) {
                    switch (counter.Name) {
                        case "Current File Cache Memory Usage":
                            fileCacheMemoryUsage += counter.Value;
                            break;
                        case "Current Files Cached":
                            currentFilesCached += counter.Value;
                            break;
                        case "Current URIs Cached":
                            currentUrisCached += counter.Value;
                            break;
                        case "File Cache Hits":
                            fileCacheHits += counter.Value;
                            break;
                        case "File Cache Misses":
                            fileCacheMisses += counter.Value;
                            break;
                        case "Output Cache Current Items":
                            outputCacheCurrentItems += counter.Value;
                            break;
                        case "Output Cache Current Memory Usage":
                            outputCacheCurrentMemoryUsage += counter.Value;
                            break;
                        case "Output Cache Total Hits":
                            outputCacheTotalHits += counter.Value;
                            break;
                        case "Output Cache Total Misses":
                            outputCacheTotalMisses += counter.Value;
                            break;
                        case "Total Files Cached":
                            totalFilesCached += counter.Value;
                            break;
                        case "Total URIs Cached":
                            totalUrisCached += counter.Value;
                            break;
                        case "URI Cache Hits":
                            uriCacheHits += counter.Value;
                            break;
                        case "URI Cache Misses":
                            uriCacheMisses += counter.Value;
                            break;
                        default:
                            break;
                    }
                }
            }

            _snapshot.BytesRecvSec = bytesRecvSec;
            _snapshot.BytesSentSec = bytesSentSec;
            _snapshot.ConnectionAttemptsSec = connectionAttemptsSec;
            _snapshot.TotalConnectionAttempts = totalConnectionAttempts;
            _snapshot.ActiveRequests = activeRequests;
            _snapshot.RequestsSec = requestsSec;
            _snapshot.TotalRequests = totalRequests;
            _snapshot.PercentCpuTime = percentCpuTime;
            _snapshot.HandleCount = handleCount;
            _snapshot.PrivateBytes = privateBytes;
            _snapshot.ThreadCount = threadCount;
            _snapshot.PrivateWorkingSet = privateWorkingSet;
            _snapshot.WorkingSet = workingSet;
            _snapshot.IOReadSec = IOReadSec;
            _snapshot.IOWriteSec = IOWriteSec;
            _snapshot.FileCacheHits = fileCacheHits;
            _snapshot.FileCacheMisses = fileCacheMisses;
            _snapshot.PageFaultsSec = pageFaultsSec;
            _snapshot.Percent500 = percent500;
            _snapshot.AvailableBytes = availableBytes;
            _snapshot.FileCacheMemoryUsage = fileCacheMemoryUsage;
            _snapshot.CurrentFilesCached = currentFilesCached;
            _snapshot.CurrentUrisCached = currentUrisCached;
            _snapshot.FileCacheHits = fileCacheHits;
            _snapshot.FileCacheMisses = fileCacheMisses;
            _snapshot.OutputCacheCurrentItems = outputCacheCurrentItems;
            _snapshot.OutputCacheMemoryUsage = outputCacheCurrentMemoryUsage;
            _snapshot.OutputCacheTotalHits = outputCacheTotalHits;
            _snapshot.OutputCacheTotalMisses = outputCacheTotalMisses;
            _snapshot.TotalFilesCached = totalFilesCached;
            _snapshot.TotalUrisCached = totalUrisCached;
            _snapshot.UriCacheHits = uriCacheHits;
            _snapshot.UriCacheMisses = uriCacheMisses;


            _snapshot.ProcessCount = _webserverProcesses.Count();

            _lastCalculation = DateTime.UtcNow;
        }

        private async Task Reset()
        {
            Dispose();
            await Initialize();
        }

        private async Task Query()
        {
            for (int i = 0; i < 5; i++) {
                try {
                    await _counterMonitor.Refresh();
                    return;
                }
                catch (CounterNotFoundException) {
                    await Task.Delay(20);
                    await Reset();
                }
            }

            await _counterMonitor.Refresh();
        }

        private bool HasChanged()
        {

            if (!Enumerable.SequenceEqual(_allProcesses, Process.GetProcesses().Select(p => p.Id))) {

                if (!Enumerable.SequenceEqual(_webserverProcesses, ProcessUtil.GetWebserverProcessIds().OrderBy(id => id))) {
                    return true;
                }
            }

            return GetWebserverCounters().Count() != _webserverCounters.Count();
        }

        private IEnumerable<IPerfCounter> GetWebserverCounters()
        {
            List<IPerfCounter> counters = new List<IPerfCounter>();
            const string TotalInstance = "_Total";

            // Only use _total counter if instances are available
            if (_provider.GetInstances(WebSiteCategory).Where(i => i != TotalInstance).Count() > 0) {
                counters.AddRange(_provider.GetCounters(WebSiteCategory, TotalInstance));
            }

            if (_provider.GetInstances(WorkerProcessCategory).Where(i => i != TotalInstance).Count() > 0) {
                counters.AddRange(_provider.GetCounters(WorkerProcessCategory, TotalInstance));
            }

            return counters;
        }

        private IEnumerable<IPerfCounter> GetMemoryCounters()
        {
            return _provider.GetSingletonCounters(MemoryCategory);
        }

        private IEnumerable<IPerfCounter> GetCacheCounters()
        {
            return _provider.GetSingletonCounters(CacheCategory);
        }
    }
}
