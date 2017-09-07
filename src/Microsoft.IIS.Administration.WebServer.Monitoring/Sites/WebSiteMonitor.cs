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

    class WebSiteMonitor : IWebSiteMonitor, IDisposable
    {
        CounterProvider _provider = new CounterProvider();
        CounterMonitor _monitor;
        Dictionary<string, List<IPerfCounter>> _calculationCache;

        public async Task <IEnumerable<IWebSiteSnapshot>> GetSnapshots(IEnumerable<Site> sites)
        {
            if (_monitor == null) {
                Initialize();
            }
            
            if (_monitor.Counters.Select(c => c.InstanceName).Distinct().Count() < sites.Count()) {
                Reset();
            }

            await Query();

            var snapshots = new List<WebSiteSnapshot>();

            foreach (var site in sites) {
                snapshots.Add(GetSnapShot(site));
            }

            return snapshots;
        }

        public void Dispose()
        {
            _calculationCache = null;

            if (_monitor != null) {
                _monitor.Dispose();
                _monitor = null;
            }
        }

        private void Initialize()
        {
            _monitor = new CounterMonitor(_provider.GetCounters("Web Service"));

            _calculationCache = new Dictionary<string, List<IPerfCounter>>();

            foreach (IPerfCounter counter in _monitor.Counters) {

                if (!_calculationCache.ContainsKey(counter.InstanceName)) {
                    _calculationCache.Add(counter.InstanceName, new List<IPerfCounter>());
                }

                _calculationCache[counter.InstanceName].Add(counter);
            }
        }

        private void Reset()
        {
            Dispose();
            Initialize();
        }

        private WebSiteSnapshot GetSnapShot(Site site)
        {
            var snapshot = new WebSiteSnapshot();
            snapshot.Name = site.Name;

            if (_calculationCache.TryGetValue(site.Name, out List<IPerfCounter> counters)) {
                foreach (var counter in counters) {
                    switch (counter.Name) {
                        case "Service Uptime":
                            snapshot.Uptime = counter.Value;
                            break;
                        case "Bytes Received/sec":
                            snapshot.BytesRecvSec += counter.Value;
                            break;
                        case "Bytes Sent/sec":
                            snapshot.BytesSentSec += counter.Value;
                            break;
                        case "Connection Attempts/sec":
                            snapshot.ConnectionAttemptsSec += counter.Value;
                            break;
                        case "Current Connections":
                            snapshot.CurrentConnections += counter.Value;
                            break;
                        case "Total Connection Attempts (all instances)":
                            snapshot.TotalConnectionAttempts += counter.Value;
                            break;

                        //
                        // Requests / sec, no fall through
                        case "Total Method Requests/sec":
                            snapshot.TotalRequestsSec += counter.Value;
                            break;
                        case "Other Request Methods/sec":
                            snapshot.TotalRequestsSec += counter.Value;
                            break;

                        //
                        // Total requests, no fall through case
                        case "Total Method Requests":
                            snapshot.TotalRequests += counter.Value;
                            break;
                        case "Total Other Request Methods":
                            snapshot.TotalRequests += counter.Value;
                            break;
                        default:
                            break;
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
                    Reset();
                }
            }

            await _monitor.Refresh();
        }
    }
}
