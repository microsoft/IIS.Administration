// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Monitoring
{
    using Microsoft.IIS.Administration.Monitoring;
    using Microsoft.Web.Administration;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    class WebSiteMonitor : IWebSiteMonitor
    {
        private CounterProvider _counterProvider;
        private Dictionary<int, string> _processCounterInstances = null;

        public WebSiteMonitor(CounterProvider counterProvider)
        {
            _counterProvider = counterProvider;
        }

        public async Task <IEnumerable<IWebSiteSnapshot>> GetSnapshots(IEnumerable<Site> sites)
        {
            if (_processCounterInstances == null) {
                _processCounterInstances = await ProcessUtil.GetProcessCounterMap(_counterProvider, "W3WP");
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

            foreach (var counter in counters) {
                if (counter.CategoryName.Equals(WebSiteCounterNames.Category)) {
                    switch (counter.Name) {
                        case WebSiteCounterNames.ServiceUptime:
                            snapshot.Uptime = counter.Value;
                            break;
                        case WebSiteCounterNames.BytesRecvSec:
                            snapshot.BytesRecvSec += counter.Value;
                            break;
                        case WebSiteCounterNames.BytesSentSec:
                            snapshot.BytesSentSec += counter.Value;
                            break;
                        case WebSiteCounterNames.ConnectionAttemptsSec:
                            snapshot.ConnectionAttemptsSec += counter.Value;
                            break;
                        case WebSiteCounterNames.CurrentConnections:
                            snapshot.CurrentConnections += counter.Value;
                            break;
                        case WebSiteCounterNames.TotalConnectionAttempts:
                            snapshot.TotalConnectionAttempts += counter.Value;
                            break;
                        case WebSiteCounterNames.TotalMethodRequestsSec:
                            snapshot.TotalRequestsSec += counter.Value;
                            break;
                        case WebSiteCounterNames.TotalOtherMethodRequestsSec:
                            snapshot.TotalRequestsSec += counter.Value;
                            break;
                        case WebSiteCounterNames.TotalMethodRequests:
                            snapshot.TotalRequests += counter.Value;
                            break;
                        case WebSiteCounterNames.TotalOtherMethodRequests:
                            snapshot.TotalRequests += counter.Value;
                            break;
                        default:
                            break;
                    }
                }

                if (counter.CategoryName.Equals(WorkerProcessCounterNames.Category)) {
                    switch (counter.Name) {
                        case WorkerProcessCounterNames.ActiveRequests:
                            snapshot.ActiveRequests += counter.Value;
                            break;
                        case WorkerProcessCounterNames.Percent500:
                            snapshot.Percent500 += counter.Value;
                            break;
                        case WorkerProcessCounterNames.RequestsSec:
                            snapshot.RequestsSec += counter.Value;
                            break;
                        case WorkerProcessCounterNames.TotalRequests:
                            snapshot.TotalRequests += counter.Value;
                            break;
                        case WorkerProcessCounterNames.CurrentFileCacheMemoryUsage:
                            snapshot.FileCacheMemoryUsage += counter.Value;
                            break;
                        case WorkerProcessCounterNames.CurrentFilesCached:
                            snapshot.CurrentFilesCached += counter.Value;
                            break;
                        case WorkerProcessCounterNames.CurrentUrisCached:
                            snapshot.CurrentUrisCached += counter.Value;
                            break;
                        case WorkerProcessCounterNames.FileCacheHits:
                            snapshot.FileCacheHits += counter.Value;
                            break;
                        case WorkerProcessCounterNames.FileCacheMisses:
                            snapshot.FileCacheMisses += counter.Value;
                            break;
                        case WorkerProcessCounterNames.OutputCacheCurrentItems:
                            snapshot.OutputCacheCurrentItems += counter.Value;
                            break;
                        case WorkerProcessCounterNames.OutputCacheCurrentMemoryUsage:
                            snapshot.OutputCacheCurrentMemoryUsage += counter.Value;
                            break;
                        case WorkerProcessCounterNames.OutputCacheTotalHits:
                            snapshot.OutputCacheTotalHits += counter.Value;
                            break;
                        case WorkerProcessCounterNames.OutputCacheTotalMisses:
                            snapshot.OutputCacheTotalMisses += counter.Value;
                            break;
                        case WorkerProcessCounterNames.TotalFilesCached:
                            snapshot.TotalFilesCached += counter.Value;
                            break;
                        case WorkerProcessCounterNames.TotalUrisCached:
                            snapshot.TotalUrisCached += counter.Value;
                            break;
                        case WorkerProcessCounterNames.UriCacheHits:
                            snapshot.UriCacheHits += counter.Value;
                            break;
                        case WorkerProcessCounterNames.UriCacheMisses:
                            snapshot.UriCacheMisses += counter.Value;
                            break;
                        default:
                            break;
                    }
                }

                if (counter.CategoryName.Equals(ProcessCounterNames.Category)) {
                    switch (counter.Name) {
                        case ProcessCounterNames.PercentCpu:
                            snapshot.PercentCpuTime += counter.Value;
                            break;
                        case ProcessCounterNames.HandleCount:
                            snapshot.HandleCount += counter.Value;
                            break;
                        case ProcessCounterNames.PrivateBytes:
                            snapshot.PrivateBytes += counter.Value;
                            break;
                        case ProcessCounterNames.ThreadCount:
                            snapshot.ThreadCount += counter.Value;
                            break;
                        case ProcessCounterNames.PrivateWorkingSet:
                            snapshot.PrivateWorkingSet += counter.Value;
                            break;
                        case ProcessCounterNames.WorkingSet:
                            snapshot.WorkingSet += counter.Value;
                            break;
                        case ProcessCounterNames.IOReadSec:
                            snapshot.IOReadSec += counter.Value;
                            break;
                        case ProcessCounterNames.IOWriteSec:
                            snapshot.IOWriteSec += counter.Value;
                            break;
                        case ProcessCounterNames.PageFaultsSec:
                            snapshot.PageFaultsSec += counter.Value;
                            break;
                        default:
                            break;
                    }
                }
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

            counters.AddRange(await _counterProvider.GetCounters(WebSiteCounterNames.Category, site.Name, WebSiteCounterNames.CounterNames));

            Application rootApp = site.Applications["/"];

            ApplicationPool pool = rootApp != null ? AppPools.AppPoolHelper.GetAppPool(rootApp.ApplicationPoolName) : null;

            if (pool != null) {
                var wps = pool.GetWorkerProcesses();

                foreach (WorkerProcess wp in wps) {
                    if (!_processCounterInstances.ContainsKey(wp.ProcessId)) {
                        _processCounterInstances = await ProcessUtil.GetProcessCounterMap(_counterProvider, "W3WP");

                        //
                        // Counter instance doesn't exist
                        if (!_processCounterInstances.ContainsKey(wp.ProcessId)) {
                            continue;
                        }
                    }

                    counters.AddRange(await _counterProvider.GetCounters(ProcessCounterNames.Category, _processCounterInstances[wp.ProcessId], ProcessCounterNames.CounterNames));
                }

                foreach (WorkerProcess wp in wps) {
                    counters.AddRange(await _counterProvider.GetCounters(
                        WorkerProcessCounterNames.Category,
                        WorkerProcessCounterNames.GetInstanceName(wp.ProcessId, pool.Name),
                        WorkerProcessCounterNames.CounterNames));
                }
            }

            return counters;
        }
    }
}
