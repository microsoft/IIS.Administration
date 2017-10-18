// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Monitoring
{
    using Microsoft.IIS.Administration.Monitoring;
    using Microsoft.Web.Administration;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    class WebSiteMonitor : IWebSiteMonitor
    {
        private CounterProvider _counterProvider;
        private static readonly int _processorCount = Environment.ProcessorCount;
        private Dictionary<int, string> _processCounterInstances = null;
        private Dictionary<string, int> _siteProcessCounts = new Dictionary<string, int>();
        private ProcessCounterNames _processCounterNames = null;
        private WebSiteCounterNames _webSiteCounterNames = null;
        private WorkerProcessCounterNames _workerProcessCounterNames = null;
        private MemoryCounterNames _memoryCounterNames = null;

        public WebSiteMonitor(CounterProvider counterProvider, ICounterTranslator translator)
        {
            _counterProvider = counterProvider;
            _processCounterNames = new ProcessCounterNames(translator);
            _webSiteCounterNames = new WebSiteCounterNames(translator);
            _workerProcessCounterNames = new WorkerProcessCounterNames(translator);
            _memoryCounterNames = new MemoryCounterNames(translator);
        }

        public async Task <IEnumerable<IWebSiteSnapshot>> GetSnapshots(IEnumerable<Site> sites)
        {
            if (_processCounterInstances == null) {
                _processCounterInstances = await ProcessUtil.GetProcessCounterMap(_processCounterNames, _counterProvider, "W3WP");
            }

            var snapshots = new List<WebSiteSnapshot>();

            foreach (var site in sites) {
                snapshots.Add(await GetSnapShot(site));
            }

            return snapshots;
        }

        private async Task<WebSiteSnapshot> GetSnapShot(Site site)
        {
            var snapshot = new WebSiteSnapshot();
            snapshot.Name = site.Name;

            var counters = await Query(site);

            long percentCpu = 0;

            foreach (var counter in counters) {
                if (counter.CategoryName.Equals(_webSiteCounterNames.Category)) {
                    if (counter.Name == _webSiteCounterNames.ServiceUptime) {
                        snapshot.Uptime = counter.Value;
                    }
                    else if (counter.Name == _webSiteCounterNames.BytesRecvSec) {
                        snapshot.BytesRecvSec += counter.Value;
                    }
                    else if (counter.Name == _webSiteCounterNames.BytesSentSec) {
                        snapshot.BytesSentSec += counter.Value;
                    }
                    else if (counter.Name == _webSiteCounterNames.TotalBytesRecv) {
                        snapshot.TotalBytesRecv += counter.Value;
                    }
                    else if (counter.Name == _webSiteCounterNames.TotalBytesSent) {
                        snapshot.TotalBytesSent += counter.Value;
                    }
                    else if (counter.Name == _webSiteCounterNames.ConnectionAttemptsSec) {
                        snapshot.ConnectionAttemptsSec += counter.Value;
                    }
                    else if (counter.Name == _webSiteCounterNames.CurrentConnections) {
                        snapshot.CurrentConnections += counter.Value;
                    }
                    else if (counter.Name == _webSiteCounterNames.TotalConnectionAttempts) {
                        snapshot.TotalConnectionAttempts += counter.Value;
                    }
                    else if (counter.Name == _webSiteCounterNames.TotalMethodRequestsSec) {
                        snapshot.RequestsSec += counter.Value;
                    }
                    else if (counter.Name == _webSiteCounterNames.TotalOtherMethodRequestsSec) {
                        snapshot.RequestsSec += counter.Value;
                    }
                    else if (counter.Name == _webSiteCounterNames.TotalMethodRequests) {
                        snapshot.TotalRequests += counter.Value;
                    }
                    else if (counter.Name == _webSiteCounterNames.TotalOtherMethodRequests) {
                        snapshot.TotalRequests += counter.Value;
                    }
                }

                else if (counter.CategoryName.Equals(_workerProcessCounterNames.Category)) {
                    if (counter.Name == _workerProcessCounterNames.ActiveRequests) {
                        snapshot.ActiveRequests += counter.Value;
                    }
                    else if (counter.Name == _workerProcessCounterNames.CurrentFileCacheMemoryUsage) {
                        snapshot.FileCacheMemoryUsage += counter.Value;
                    }
                    else if (counter.Name == _workerProcessCounterNames.CurrentFilesCached) {
                        snapshot.CurrentFilesCached += counter.Value;
                    }
                    else if (counter.Name == _workerProcessCounterNames.CurrentUrisCached) {
                        snapshot.CurrentUrisCached += counter.Value;
                    }
                    else if (counter.Name == _workerProcessCounterNames.FileCacheHits) {
                        snapshot.FileCacheHits += counter.Value;
                    }
                    else if (counter.Name == _workerProcessCounterNames.FileCacheMisses) {
                        snapshot.FileCacheMisses += counter.Value;
                    }
                    else if (counter.Name == _workerProcessCounterNames.OutputCacheCurrentItems) {
                        snapshot.OutputCacheCurrentItems += counter.Value;
                    }
                    else if (counter.Name == _workerProcessCounterNames.OutputCacheCurrentMemoryUsage) {
                        snapshot.OutputCacheCurrentMemoryUsage += counter.Value;
                    }
                    else if (counter.Name == _workerProcessCounterNames.OutputCacheTotalHits) {
                        snapshot.OutputCacheTotalHits += counter.Value;
                    }
                    else if (counter.Name == _workerProcessCounterNames.OutputCacheTotalMisses) {
                        snapshot.OutputCacheTotalMisses += counter.Value;
                    }
                    else if (counter.Name == _workerProcessCounterNames.TotalFilesCached) {
                        snapshot.TotalFilesCached += counter.Value;
                    }
                    else if (counter.Name == _workerProcessCounterNames.TotalUrisCached) {
                        snapshot.TotalUrisCached += counter.Value;
                    }
                    else if (counter.Name == _workerProcessCounterNames.UriCacheHits) {
                        snapshot.UriCacheHits += counter.Value;
                    }
                    else if (counter.Name == _workerProcessCounterNames.UriCacheMisses) {
                        snapshot.UriCacheMisses += counter.Value;
                    }
                }

                else if (counter.CategoryName.Equals(_memoryCounterNames.Category)) {
                    if (counter.Name == _memoryCounterNames.AvailableBytes) {
                        snapshot.AvailableMemory += counter.Value;
                    }
                }

                else if (counter.CategoryName.Equals(_processCounterNames.Category)) {
                    if (counter.Name == _processCounterNames.PercentCpu) {
                        percentCpu += counter.Value;
                    }
                    else if (counter.Name == _processCounterNames.HandleCount) {
                        snapshot.HandleCount += counter.Value;
                    }
                    else if (counter.Name == _processCounterNames.PrivateBytes) {
                        snapshot.PrivateBytes += counter.Value;
                    }
                    else if (counter.Name == _processCounterNames.ThreadCount) {
                        snapshot.ThreadCount += counter.Value;
                    }
                    else if (counter.Name == _processCounterNames.PrivateWorkingSet) {
                        snapshot.PrivateWorkingSet += counter.Value;
                    }
                    else if (counter.Name == _processCounterNames.WorkingSet) {
                        snapshot.WorkingSet += counter.Value;
                    }
                    else if (counter.Name == _processCounterNames.IOReadSec) {
                        snapshot.IOReadSec += counter.Value;
                    }
                    else if (counter.Name == _processCounterNames.IOWriteSec) {
                        snapshot.IOWriteSec += counter.Value;
                    }
                    else if (counter.Name == _processCounterNames.PageFaultsSec) {
                        snapshot.PageFaultsSec += counter.Value;
                    }
                }
            }

            snapshot.PercentCpuTime = percentCpu / _processorCount;
            snapshot.TotalInstalledMemory = MemoryData.TotalInstalledMemory;
            snapshot.SystemMemoryInUse = MemoryData.TotalInstalledMemory - snapshot.AvailableMemory;

            if (_siteProcessCounts.TryGetValue(site.Name, out int count)) {
                snapshot.ProcessCount = count;
            }

            return snapshot;
        }

        private async Task<IEnumerable<IPerfCounter>> Query(Site site)
        {
            for (int i = 0; i < 5; i++) {
                try {
                    return await GetCounters(site);
                }
                catch (MissingCountersException) {
                    await Task.Delay(20);
                }
            }

            return await GetCounters(site);
        }

        private async Task<IEnumerable<IPerfCounter>> GetCounters(Site site)
        {
            var counters = new List<IPerfCounter>();

            counters.AddRange(await _counterProvider.GetCounters(_webSiteCounterNames.Category, site.Name, _webSiteCounterNames.CounterNames));

            Application rootApp = site.Applications["/"];

            ApplicationPool pool = rootApp != null ? AppPools.AppPoolHelper.GetAppPool(rootApp.ApplicationPoolName) : null;

            if (pool != null) {
                var wps = pool.GetWorkerProcesses();
                _siteProcessCounts[site.Name] = wps.Count();

                foreach (WorkerProcess wp in wps) {
                    if (!_processCounterInstances.ContainsKey(wp.ProcessId)) {
                        _processCounterInstances = await ProcessUtil.GetProcessCounterMap(_processCounterNames, _counterProvider, "W3WP");

                        //
                        // Counter instance doesn't exist
                        if (!_processCounterInstances.ContainsKey(wp.ProcessId)) {
                            continue;
                        }
                    }

                    counters.AddRange(await _counterProvider.GetCounters(_processCounterNames.Category, _processCounterInstances[wp.ProcessId], _processCounterNames.CounterNames));
                }

                foreach (WorkerProcess wp in wps) {
                    counters.AddRange(await _counterProvider.GetCounters(
                        _workerProcessCounterNames.Category,
                        WorkerProcessCounterNames.GetInstanceName(wp.ProcessId, pool.Name),
                        _workerProcessCounterNames.CounterNames));
                }
            }

            counters.AddRange(await _counterProvider.GetSingletonCounters(_memoryCounterNames.Category, _memoryCounterNames.CounterNames));

            return counters;
        }
    }
}
