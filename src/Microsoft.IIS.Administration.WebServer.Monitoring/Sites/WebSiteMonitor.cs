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

        public WebSiteMonitor(CounterProvider counterProvider)
        {
            _counterProvider = counterProvider;
        }

        public async Task <IEnumerable<IWebSiteSnapshot>> GetSnapshots(IEnumerable<Site> sites)
        {
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

            var counters = await _counterProvider.GetCounters(WebSiteCounterNames.Category, site.Name);

            foreach (var counter in counters) {
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

            return snapshot;
        }
    }
}
