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

    class AppPoolMonitor : IAppPoolMonitor
    {
        CounterProvider _counterProvider;
        private Dictionary<int, string> _processCounterInstances = null;
        private static readonly int _processorCount = Environment.ProcessorCount;
        private ProcessCounterNames _processCounterNames = null;
        private WorkerProcessCounterNames _workerProcessCounterNames = null;
        private MemoryCounterNames _memoryCounterNames = null;

        public AppPoolMonitor(CounterProvider provider, ICounterTranslator translator)
        {
            _counterProvider = provider;
            _processCounterNames = new ProcessCounterNames(translator);
            _workerProcessCounterNames = new WorkerProcessCounterNames(translator);
            _memoryCounterNames = new MemoryCounterNames(translator);
        }

        public async Task<IEnumerable<IAppPoolSnapshot>> GetSnapshots(IEnumerable<ApplicationPool> pools)
        {
            if (_processCounterInstances == null) {
                _processCounterInstances = await ProcessUtil.GetProcessCounterMap(_processCounterNames, _counterProvider, "W3WP");
            }

            var snapshots = new List<IAppPoolSnapshot>();

            foreach (var pool in pools) {
                snapshots.Add(await GetSnapShot(pool));
            }

            return snapshots;
        }

        private async Task<AppPoolSnapshot> GetSnapShot(ApplicationPool pool)
        {
            var snapshot = new AppPoolSnapshot();

            snapshot.Name = pool.Name;
            snapshot.ProcessCount = pool.GetWorkerProcesses().Count();

            long percentCpu = 0;

            foreach (IPerfCounter counter in await Query(pool)) {

                if (counter.CategoryName.Equals(_workerProcessCounterNames.Category)) {
                    if (counter.Name == _workerProcessCounterNames.ActiveRequests) {
                        snapshot.ActiveRequests += counter.Value;
                    }
                    else if (counter.Name == _workerProcessCounterNames.RequestsSec) {
                        snapshot.RequestsSec += counter.Value;
                    }
                    else if (counter.Name == _workerProcessCounterNames.TotalRequests) {
                        snapshot.TotalRequests += counter.Value;
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

                else if (counter.CategoryName.Equals(_processCounterNames.Category)) {
                    if (counter.Name == _processCounterNames.PercentCpu) {
                        snapshot.PercentCpuTime += counter.Value;
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

                else if (counter.CategoryName.Equals(_memoryCounterNames.Category)) {
                    if (counter.Name == _memoryCounterNames.AvailableBytes) {
                        snapshot.AvailableMemory += counter.Value;
                    }
                }

                snapshot.PercentCpuTime = percentCpu / _processorCount;
                snapshot.TotalInstalledMemory = MemoryData.TotalInstalledMemory;
                snapshot.SystemMemoryInUse = MemoryData.TotalInstalledMemory - snapshot.AvailableMemory;
            }

            return snapshot;
        }

        private async Task<IEnumerable<IPerfCounter>> Query(ApplicationPool pool)
        {
            for (int i = 0; i < 5; i++) {
                try {
                    return await GetCounters(pool);
                }
                catch (MissingCountersException) {
                    await Task.Delay(20);
                }
            }

            return await GetCounters(pool);
        }

        private async Task<IEnumerable<IPerfCounter>> GetCounters(ApplicationPool pool)
        {
            var poolCounters = new List<IPerfCounter>();
            var wps = pool.GetWorkerProcesses();

            foreach (WorkerProcess wp in wps) {
                if (!_processCounterInstances.ContainsKey(wp.ProcessId)) {
                    _processCounterInstances = await ProcessUtil.GetProcessCounterMap(_processCounterNames, _counterProvider, "W3WP");

                    //
                    // Counter instance doesn't exist
                    if (!_processCounterInstances.ContainsKey(wp.ProcessId)) {
                        continue;
                    }
                }

                poolCounters.AddRange(await _counterProvider.GetCounters(_processCounterNames.Category, _processCounterInstances[wp.ProcessId], _processCounterNames.CounterNames));
            }

            foreach (WorkerProcess wp in wps) {
                poolCounters.AddRange(await _counterProvider.GetCounters(
                    _workerProcessCounterNames.Category,
                    WorkerProcessCounterNames.GetInstanceName(wp.ProcessId, pool.Name),
                    _workerProcessCounterNames.CounterNames));
            }

            poolCounters.AddRange( await _counterProvider.GetSingletonCounters(_memoryCounterNames.Category, _memoryCounterNames.CounterNames));

            return poolCounters;
        }
    }

    public static class PExt
    {
        public static IEnumerable<WorkerProcess> GetWorkerProcesses(this ApplicationPool pool)
        {
            return ManagementUnit.ServerManager.WorkerProcesses.Where(wp => wp.AppPoolName == pool.Name);
        }
    }
}