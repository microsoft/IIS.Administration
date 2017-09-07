// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Monitoring
{
    using Microsoft.IIS.Administration.WebServer.Sites;
    using Microsoft.Web.Administration;

    class SiteHelper
    {
        public static object ToJsonModel(IWebSiteSnapshot snapshot, Site site)
        {
            var obj = new {
                id = new SiteId(site.Id).Uuid,
                name = snapshot.Name,
                uptime = snapshot.Uptime,
                network = new {
                    bytes_sent_sec = snapshot.BytesSentSec,
                    bytes_recv_sec = snapshot.BytesRecvSec,
                    connection_attempts_sec = snapshot.ConnectionAttemptsSec,
                    total_connection_attempts = snapshot.TotalConnectionAttempts,
                    current_connections = snapshot.CurrentConnections,
                },
                requests = new {
                    per_sec = snapshot.TotalRequestsSec,
                    total = snapshot.TotalRequests,
                },
                website = Sites.SiteHelper.ToJsonModelRef(site)
            };

            return Core.Environment.Hal.Apply(Defines.WebSiteMonitoringResource.Guid, obj);
        }
    }
}
