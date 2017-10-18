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

    class WebServerMonitor : IWebServerMonitor
    {
        private const string TotalInstance = "_Total";
        private static readonly int _processorCount = Environment.ProcessorCount;
        private IEnumerable<int> _webserverProcesses;
        private CounterProvider _provider;
        private ProcessCounterNames _processCounterNames = null;
        private WorkerProcessCounterNames _workerProcessCounterNames = null;
        private WebSiteCounterNames _webSiteCounterNames = null;
        private ProcessorCounterNames _processorCounterNames = null;
        private MemoryCounterNames _memoryCounterNames = null;
        private CacheCounterNames _cacheCounterNames = null;
        private Dictionary<int, string> _processCounterMap;

        public WebServerMonitor(CounterProvider provider, ICounterTranslator translator)
        {
            _provider = provider;
            _processCounterNames = new ProcessCounterNames(translator);
            _workerProcessCounterNames = new WorkerProcessCounterNames(translator);
            _webSiteCounterNames = new WebSiteCounterNames(translator);
            _processorCounterNames = new ProcessorCounterNames(translator);
            _memoryCounterNames = new MemoryCounterNames(translator);
            _cacheCounterNames = new CacheCounterNames(translator);
        }

        public async Task<IWebServerSnapshot> GetSnapshot()
        {
            var snapshot = new WebServerSnapshot();
            var counters = await Query();

            long bytesSentSec = 0;
            long bytesRecvSec = 0;
            long totalBytesSent = 0;
            long totalBytesRecv = 0;
            long connectionAttemptsSec = 0;
            long totalConnectionAttempts = 0;
            long currentConnections = 0;
            long activeRequests = 0;
            long requestsSec = 0;
            long totalRequests = 0;
            long percentCpuTime = 0;
            long systemPercentCpuTime = 0;
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
                if (counter.CategoryName.Equals(_webSiteCounterNames.Category)) {
                    if (counter.Name == _webSiteCounterNames.BytesRecvSec) {
                        bytesRecvSec += counter.Value;
                    }
                    else if (counter.Name == _webSiteCounterNames.BytesSentSec) {
                        bytesSentSec += counter.Value;
                    }
                    else if (counter.Name == _webSiteCounterNames.TotalBytesRecv) {
                        totalBytesRecv += counter.Value;
                    }
                    else if (counter.Name == _webSiteCounterNames.TotalBytesSent) {
                        totalBytesSent += counter.Value;
                    }
                    else if (counter.Name == _webSiteCounterNames.ConnectionAttemptsSec) {
                        connectionAttemptsSec += counter.Value;
                    }
                    else if (counter.Name == _webSiteCounterNames.TotalConnectionAttempts) {
                        totalConnectionAttempts += counter.Value;
                    }
                    else if (counter.Name == _webSiteCounterNames.CurrentConnections) {
                        currentConnections += counter.Value;
                    }
                    else if (counter.Name == _webSiteCounterNames.TotalMethodRequestsSec) {
                        requestsSec += counter.Value;
                    }
                    else if (counter.Name == _webSiteCounterNames.TotalOtherMethodRequestsSec) {
                        requestsSec += counter.Value;
                    }
                    else if (counter.Name == _webSiteCounterNames.TotalMethodRequests) {
                        totalRequests += counter.Value;
                    }
                    else if (counter.Name == _webSiteCounterNames.TotalOtherMethodRequests) {
                        totalRequests += counter.Value;
                    }
                }

                else if (counter.CategoryName.Equals(_workerProcessCounterNames.Category)) {
                    if (counter.Name == _workerProcessCounterNames.ActiveRequests) {
                        activeRequests += counter.Value;
                    }
                }

                else if (counter.CategoryName.Equals(_processCounterNames.Category)) {
                    if (counter.Name == _processCounterNames.PercentCpu) {
                        percentCpuTime += counter.Value;
                    }
                    else if (counter.Name == _processCounterNames.HandleCount) {
                        handleCount += counter.Value;
                    }
                    else if (counter.Name == _processCounterNames.PrivateBytes) {
                        privateBytes += counter.Value;
                    }
                    else if (counter.Name == _processCounterNames.ThreadCount) {
                        threadCount += counter.Value;
                    }
                    else if (counter.Name == _processCounterNames.PrivateWorkingSet) {
                        privateWorkingSet += counter.Value;
                    }
                    else if (counter.Name == _processCounterNames.WorkingSet) {
                        workingSet += counter.Value;
                    }
                    else if (counter.Name == _processCounterNames.IOReadSec) {
                        IOReadSec += counter.Value;
                    }
                    else if (counter.Name == _processCounterNames.IOWriteSec) {
                        IOWriteSec += counter.Value;
                    }
                    else if (counter.Name == _processCounterNames.PageFaultsSec) {
                        pageFaultsSec += counter.Value;
                    }
                }

                else if (counter.CategoryName.Equals(_processorCounterNames.Category)) {
                    if (counter.Name == _processorCounterNames.IdleTime) {
                        systemPercentCpuTime += 100 - counter.Value;
                    }
                }

                else if (counter.CategoryName.Equals(_memoryCounterNames.Category)) {
                    if (counter.Name == _memoryCounterNames.AvailableBytes) {
                        availableBytes += counter.Value;
                    }
                }

                else if (counter.CategoryName.Equals(_cacheCounterNames.Category)) {
                    if (counter.Name == _cacheCounterNames.CurrentFileCacheMemoryUsage) {
                        fileCacheMemoryUsage += counter.Value;
                    }
                    else if (counter.Name == _cacheCounterNames.CurrentFilesCached) {
                        currentFilesCached += counter.Value;
                    }
                    else if (counter.Name == _cacheCounterNames.CurrentUrisCached) {
                        currentUrisCached += counter.Value;
                    }
                    else if (counter.Name == _cacheCounterNames.FileCacheHits) {
                        fileCacheHits += counter.Value;
                    }
                    else if (counter.Name == _cacheCounterNames.FileCacheMisses) {
                        fileCacheMisses += counter.Value;
                    }
                    else if (counter.Name == _cacheCounterNames.OutputCacheCurrentItems) {
                        outputCacheCurrentItems += counter.Value;
                    }
                    else if (counter.Name == _cacheCounterNames.OutputCacheCurrentMemoryUsage) {
                        outputCacheCurrentMemoryUsage += counter.Value;
                    }
                    else if (counter.Name == _cacheCounterNames.OutputCacheTotalHits) {
                        outputCacheTotalHits += counter.Value;
                    }
                    else if (counter.Name == _cacheCounterNames.OutputCacheTotalMisses) {
                        outputCacheTotalMisses += counter.Value;
                    }
                    else if (counter.Name == _cacheCounterNames.TotalFilesCached) {
                        totalFilesCached += counter.Value;
                    }
                    else if (counter.Name == _cacheCounterNames.TotalUrisCached) {
                        totalUrisCached += counter.Value;
                    }
                    else if (counter.Name == _cacheCounterNames.UriCacheHits) {
                        uriCacheHits += counter.Value;
                    }
                    else if (counter.Name == _cacheCounterNames.UriCacheMisses) {
                        uriCacheMisses += counter.Value;
                    }
                }
            }

            snapshot.BytesRecvSec = bytesRecvSec;
            snapshot.BytesSentSec = bytesSentSec;
            snapshot.TotalBytesSent = totalBytesSent;
            snapshot.TotalBytesRecv = totalBytesRecv;
            snapshot.ConnectionAttemptsSec = connectionAttemptsSec;
            snapshot.TotalConnectionAttempts = totalConnectionAttempts;
            snapshot.CurrentConnections = currentConnections;
            snapshot.ActiveRequests = activeRequests;
            snapshot.RequestsSec = requestsSec;
            snapshot.TotalRequests = totalRequests;
            snapshot.PercentCpuTime = percentCpuTime / _processorCount;
            snapshot.SystemPercentCpuTime = systemPercentCpuTime;
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
            snapshot.AvailableMemory = availableBytes;
            snapshot.TotalInstalledMemory = MemoryData.TotalInstalledMemory;
            snapshot.SystemMemoryInUse = MemoryData.TotalInstalledMemory - availableBytes;
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
            var counterFinder = new CounterFinder();
            List<IPerfCounter> counters = new List<IPerfCounter>();
            _webserverProcesses = ProcessUtil.GetWebserverProcessIds();

            // Only use total counter if instances are available
            if (counterFinder.GetInstances(_webSiteCounterNames.Category).Any(i => i != TotalInstance)) {
                counters.AddRange(await _provider.GetCounters(_webSiteCounterNames.Category, TotalInstance, _webSiteCounterNames.CounterNames));
            }

            if (counterFinder.GetInstances(_workerProcessCounterNames.Category).Any(i => i != TotalInstance)) {
                counters.AddRange(await _provider.GetCounters(_workerProcessCounterNames.Category, TotalInstance, _workerProcessCounterNames.CounterNames));
            }

            counters.AddRange(await _provider.GetSingletonCounters(_memoryCounterNames.Category, _memoryCounterNames.CounterNames));

            counters.AddRange(await _provider.GetSingletonCounters(_cacheCounterNames.Category, _cacheCounterNames.CounterNames));

            counters.AddRange(await _provider.GetCounters(_processorCounterNames.Category, TotalInstance, _processorCounterNames.CounterNames));

            if (_processCounterMap == null) {
                _processCounterMap = await ProcessUtil.GetProcessCounterMap(_processCounterNames, _provider, "w3wp");
            }

            foreach (int processId in _webserverProcesses) {

                string instanceName = await TryGetProcessCounterInstance(processId);

                if (instanceName != null) {
                    counters.AddRange(await _provider.GetCounters(_processCounterNames.Category, instanceName, _processCounterNames.CounterNames));
                }
            }

            return counters;
        }

        private async Task<string> TryGetProcessCounterInstance(int id)
        {
            if (!_processCounterMap.TryGetValue(id, out string instanceName)) {

                Process p = Process.GetProcessById(id);
                
                if (p != null) {

                    var map = await ProcessUtil.GetProcessCounterMap(_processCounterNames, _provider, p.ProcessName);

                    foreach (int key in map.Keys) {
                        _processCounterMap[key] = map[key];
                    }

                    if (_processCounterMap.TryGetValue(id, out instanceName)) {
                        return instanceName;
                    }
                }
            }
            else {
                return instanceName;
            }

            return null;
        }
    }
}
