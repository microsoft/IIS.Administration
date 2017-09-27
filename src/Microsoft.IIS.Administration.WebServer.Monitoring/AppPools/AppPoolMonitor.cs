﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Monitoring
{
    using Microsoft.IIS.Administration.Monitoring;
    using Microsoft.Web.Administration;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    class AppPoolMonitor : IAppPoolMonitor
    {
        CounterProvider _provider = new CounterProvider();
        private IEnumerable<int> _allProcesses;
        private IEnumerable<int> _workerProcesses;
        private readonly TimeSpan RefreshRate = TimeSpan.FromSeconds(1);
        private DateTime _lastChangeDetect;
        CounterMonitor _monitor;
        Dictionary<string, List<IPerfCounter>> _calculationCache;

        public async Task<IEnumerable<IAppPoolSnapshot>> GetSnapshots(IEnumerable<ApplicationPool> pools)
        {
            if (_monitor == null) {
                await Initialize();
            }

            if ((DateTime.UtcNow - _lastChangeDetect > RefreshRate) && HasChanged()) {
                await Reset();
                _lastChangeDetect = DateTime.UtcNow;
            }

            await Query();

            var snapshots = new List<IAppPoolSnapshot>();

            foreach (var pool in pools) {
                snapshots.Add(GetSnapShot(pool));
            }

            return snapshots;
        }

        public void Dispose()
        {
            _calculationCache = null;

            if (_monitor != null) {
                var monitor = _monitor;
                _monitor = null;
                monitor.Dispose();
            }
        }

        private async Task Initialize()
        {
            //
            // Set up tracking for change detection
            _allProcesses = Process.GetProcesses().Select(p => p.Id);
            _workerProcesses = ManagementUnit.ServerManager.WorkerProcesses.Select(wp => wp.ProcessId).OrderBy(id => id);

            //
            // Set up counters for data calculation
            _calculationCache = new Dictionary<string, List<IPerfCounter>>();
            Dictionary<string, List<IPerfCounter>> processCounterMap = new Dictionary<string, List<IPerfCounter>>();
            Dictionary<string, List<IPerfCounter>> wpCounterMap = new Dictionary<string, List<IPerfCounter>>();

            IEnumerable<IPerfCounter> wpCounters = _provider.GetCounters(WorkerProcessCounterNames.Category);
            IEnumerable<ProcessPerfCounter> processCounters = await ProcessUtil.GetProcessCounters(_workerProcesses);

            //
            // Process counters
            foreach (var counter in processCounters) {
                if (!processCounterMap.ContainsKey(counter.ProcessId.ToString())) {
                    processCounterMap.Add(counter.ProcessId.ToString(), new List<IPerfCounter>());
                }

                processCounterMap[counter.ProcessId.ToString()].Add(counter);
            }

            //
            // W3wp counters
            foreach (var counter in wpCounters) {
                if (!wpCounterMap.ContainsKey(counter.InstanceName)) {
                    wpCounterMap.Add(counter.InstanceName, new List<IPerfCounter>());
                }

                wpCounterMap[counter.InstanceName].Add(counter);
            }

            //
            // Aggregate all relevant counters for each application pool
            foreach (ApplicationPool pool in ManagementUnit.ServerManager.ApplicationPools) {
                if (!_calculationCache.ContainsKey(pool.Name)) {
                    _calculationCache.Add(pool.Name, new List<IPerfCounter>());
                }

                List<IPerfCounter> poolCounters = _calculationCache[pool.Name];

                foreach (WorkerProcess wp in pool.GetWorkerProcesses()) {
                    if (processCounterMap.TryGetValue(wp.ProcessId.ToString(), out var procCounters)) {
                        poolCounters.AddRange(procCounters);
                    }

                    if (wpCounterMap.TryGetValue(wp.ProcessId + "_" + pool.Name, out var counters)) {
                        poolCounters.AddRange(counters);
                    }
                }
            }

            //
            // Create a counter monitor
            var allCounters = new List<IPerfCounter>();
            foreach (IEnumerable<IPerfCounter> counters in _calculationCache.Values) {
                allCounters.AddRange(counters);
            }

            _monitor = new CounterMonitor(allCounters);
        }

        private async Task Reset()
        {
            Dispose();
            await Initialize();
        }

        private AppPoolSnapshot GetSnapShot(ApplicationPool pool)
        {
            var snapshot = new AppPoolSnapshot();
            snapshot.Name = pool.Name;

            if (_calculationCache.TryGetValue(pool.Name, out List<IPerfCounter> counters)) {

                snapshot.ProcessCount = pool.GetWorkerProcesses().Count();

                foreach (var counter in counters) {

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
            }

            return snapshot;
        }

        private async Task Query()
        {
            for (int i = 0; i < 5; i++) {
                try {
                    await _monitor.Refresh();
                    return;
                }
                catch (CounterNotFoundException) {
                    await Task.Delay(20);
                    await Reset();
                }
            }

            await _monitor.Refresh();
        }

        private bool HasChanged()
        {


            if (!Enumerable.SequenceEqual(_allProcesses, Process.GetProcesses().Select(p => p.Id))) {

                if (!Enumerable.SequenceEqual(_workerProcesses, ManagementUnit.ServerManager.WorkerProcesses.Select(wp => wp.ProcessId).OrderBy(id => id))) {
                    return true;
                }
            }

            return false;
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