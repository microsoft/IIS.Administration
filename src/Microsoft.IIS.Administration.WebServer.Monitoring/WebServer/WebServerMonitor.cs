// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Monitoring
{
    using Microsoft.IIS.Administration.Monitoring;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    class WebServerMonitor : IWebServerMonitor
    {
        private IEnumerable<int> _webserverProcesses;
        private CounterProvider _provider;

        public WebServerMonitor(CounterProvider provider)
        {
            _provider = provider;
        }

        public async Task<IWebServerSnapshot> GetSnapshot()
        {
            var snapshot = new WebServerSnapshot();
            var counters = await Query();

            long bytesSentSec = 0;
            long bytesRecvSec = 0;
            long connectionAttemptsSec = 0;
            long totalConnectionAttempts = 0;
            long currentConnections = 0;
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

            foreach (IPerfCounter counter in counters) {
                if (counter.CategoryName.Equals(WebSiteCounterNames.Category)) {
                    switch (counter.Name) {
                        case WebSiteCounterNames.BytesRecvSec:
                            bytesRecvSec += counter.Value;
                            break;
                        case WebSiteCounterNames.BytesSentSec:
                            bytesSentSec += counter.Value;
                            break;
                        case WebSiteCounterNames.ConnectionAttemptsSec:
                            connectionAttemptsSec += counter.Value;
                            break;
                        case WebSiteCounterNames.TotalConnectionAttempts:
                            totalConnectionAttempts += counter.Value;
                            break;
                        case WebSiteCounterNames.CurrentConnections:
                            currentConnections += counter.Value;
                            break;
                        case WebSiteCounterNames.TotalMethodRequestsSec:
                            requestsSec += counter.Value;
                            break;
                        case WebSiteCounterNames.TotalOtherMethodRequestsSec:
                            requestsSec += counter.Value;
                            break;
                        case WebSiteCounterNames.TotalMethodRequests:
                            totalRequests += counter.Value;
                            break;
                        case WebSiteCounterNames.TotalOtherMethodRequests:
                            totalRequests += counter.Value;
                            break;
                        default:
                            break;
                    }
                }

                if (counter.CategoryName.Equals(WorkerProcessCounterNames.Category)) {
                    switch (counter.Name) {
                        case WorkerProcessCounterNames.ActiveRequests:
                            activeRequests += counter.Value;
                            break;
                        default:
                            break;
                    }
                }

                if (counter.CategoryName.Equals(ProcessCounterNames.Category)) {
                    switch (counter.Name) {
                        case ProcessCounterNames.PercentCpu:
                            percentCpuTime += counter.Value;
                            break;
                        case ProcessCounterNames.HandleCount:
                            handleCount += counter.Value;
                            break;
                        case ProcessCounterNames.PrivateBytes:
                            privateBytes += counter.Value;
                            break;
                        case ProcessCounterNames.ThreadCount:
                            threadCount += counter.Value;
                            break;
                        case ProcessCounterNames.PrivateWorkingSet:
                            privateWorkingSet += counter.Value;
                            break;
                        case ProcessCounterNames.WorkingSet:
                            workingSet += counter.Value;
                            break;
                        case ProcessCounterNames.IOReadSec:
                            IOReadSec += counter.Value;
                            break;
                        case ProcessCounterNames.IOWriteSec:
                            IOWriteSec += counter.Value;
                            break;
                        case ProcessCounterNames.PageFaultsSec:
                            pageFaultsSec += counter.Value;
                            break;
                        default:
                            break;
                    }
                }

                if (counter.CategoryName.Equals(MemoryCounterNames.Category)) {
                    switch (counter.Name) {
                        case MemoryCounterNames.AvailableBytes:
                            availableBytes += counter.Value;
                            break;
                        default:
                            break;
                    }
                }

                if (counter.CategoryName.Equals(CacheCounterNames.Category)) {
                    switch (counter.Name) {
                        case CacheCounterNames.CurrentFileCacheMemoryUsage:
                            fileCacheMemoryUsage += counter.Value;
                            break;
                        case CacheCounterNames.CurrentFilesCached:
                            currentFilesCached += counter.Value;
                            break;
                        case CacheCounterNames.CurrentUrisCached:
                            currentUrisCached += counter.Value;
                            break;
                        case CacheCounterNames.FileCacheHits:
                            fileCacheHits += counter.Value;
                            break;
                        case CacheCounterNames.FileCacheMisses:
                            fileCacheMisses += counter.Value;
                            break;
                        case CacheCounterNames.OutputCacheCurrentItems:
                            outputCacheCurrentItems += counter.Value;
                            break;
                        case CacheCounterNames.OutputCacheCurrentMemoryUsage:
                            outputCacheCurrentMemoryUsage += counter.Value;
                            break;
                        case CacheCounterNames.OutputCacheTotalHits:
                            outputCacheTotalHits += counter.Value;
                            break;
                        case CacheCounterNames.OutputCacheTotalMisses:
                            outputCacheTotalMisses += counter.Value;
                            break;
                        case CacheCounterNames.TotalFilesCached:
                            totalFilesCached += counter.Value;
                            break;
                        case CacheCounterNames.TotalUrisCached:
                            totalUrisCached += counter.Value;
                            break;
                        case CacheCounterNames.UriCacheHits:
                            uriCacheHits += counter.Value;
                            break;
                        case CacheCounterNames.UriCacheMisses:
                            uriCacheMisses += counter.Value;
                            break;
                        default:
                            break;
                    }
                }
            }

            snapshot.BytesRecvSec = bytesRecvSec;
            snapshot.BytesSentSec = bytesSentSec;
            snapshot.ConnectionAttemptsSec = connectionAttemptsSec;
            snapshot.TotalConnectionAttempts = totalConnectionAttempts;
            snapshot.CurrentConnections = currentConnections;
            snapshot.ActiveRequests = activeRequests;
            snapshot.RequestsSec = requestsSec;
            snapshot.TotalRequests = totalRequests;
            snapshot.PercentCpuTime = percentCpuTime;
            snapshot.HandleCount = handleCount;
            snapshot.PrivateBytes = privateBytes;
            snapshot.ThreadCount = threadCount;
            snapshot.PrivateWorkingSet = privateWorkingSet;
            snapshot.WorkingSet = workingSet;
            snapshot.IOReadSec = IOReadSec;
            snapshot.IOWriteSec = IOWriteSec;
            snapshot.FileCacheHits = fileCacheHits;
            snapshot.FileCacheMisses = fileCacheMisses;
            snapshot.PageFaultsSec = pageFaultsSec;
            snapshot.AvailableBytes = availableBytes;
            snapshot.FileCacheMemoryUsage = fileCacheMemoryUsage;
            snapshot.CurrentFilesCached = currentFilesCached;
            snapshot.CurrentUrisCached = currentUrisCached;
            snapshot.FileCacheHits = fileCacheHits;
            snapshot.FileCacheMisses = fileCacheMisses;
            snapshot.OutputCacheCurrentItems = outputCacheCurrentItems;
            snapshot.OutputCacheMemoryUsage = outputCacheCurrentMemoryUsage;
            snapshot.OutputCacheTotalHits = outputCacheTotalHits;
            snapshot.OutputCacheTotalMisses = outputCacheTotalMisses;
            snapshot.TotalFilesCached = totalFilesCached;
            snapshot.TotalUrisCached = totalUrisCached;
            snapshot.UriCacheHits = uriCacheHits;
            snapshot.UriCacheMisses = uriCacheMisses;

            snapshot.ProcessCount = _webserverProcesses.Count();

            return snapshot;
        }

        private async Task<IEnumerable<IPerfCounter>> Query()
        {
            for (int i = 0; i < 5; i++) {
                try {
                    return await GetCounters();
                }
                catch (MissingCountersException) {
                    await Task.Delay(20);
                }
            }

            return await GetCounters();
        }

        private async Task<IEnumerable<IPerfCounter>> GetCounters()
        {
            const string TotalInstance = "_Total";
            var counterFinder = new CounterFinder();
            List<IPerfCounter> counters = new List<IPerfCounter>();
            _webserverProcesses = ProcessUtil.GetWebserverProcessIds().OrderBy(id => id);

            // Only use total counter if instances are available
            if (counterFinder.GetInstances(WebSiteCounterNames.Category).Any(i => i != TotalInstance)) {
                counters.AddRange(await _provider.GetCounters(WebSiteCounterNames.Category, TotalInstance, WebSiteCounterNames.CounterNames));
            }

            if (counterFinder.GetInstances(WorkerProcessCounterNames.Category).Any(i => i != TotalInstance)) {
                counters.AddRange(await _provider.GetCounters(WorkerProcessCounterNames.Category, TotalInstance, WorkerProcessCounterNames.CounterNames));
            }
            
            counters.AddRange(await _provider.GetSingletonCounters(MemoryCounterNames.Category, MemoryCounterNames.CounterNames));

            counters.AddRange(await _provider.GetSingletonCounters(CacheCounterNames.Category, CacheCounterNames.CounterNames));
            
            foreach (string instance in await ProcessUtil.GetProcessCounterInstances(_webserverProcesses)) {
                counters.AddRange(await _provider.GetCounters(ProcessCounterNames.Category, instance, ProcessCounterNames.CounterNames));
            }

            return counters;
        }
    }
}
