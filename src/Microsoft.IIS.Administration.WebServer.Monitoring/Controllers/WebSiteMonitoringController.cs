// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Monitoring
{
    using Core.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.IIS.Administration.Core;
    using Microsoft.IIS.Administration.WebServer.Sites;
    using Microsoft.Web.Administration;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    [RequireWebServer]
    public class WebSiteMonitoringController : ApiBaseController
    {
        private IWebSiteMonitor _monitor;

        public WebSiteMonitoringController(IWebSiteMonitor monitor)
        {
            _monitor = monitor;
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.WebSiteMonitoringName)]
        public async Task<object> Get()
        {
            Dictionary<string, Site> sites = ManagementUnit.ServerManager.Sites.ToDictionary(s => s.Name);
            IEnumerable<IWebSiteSnapshot> snapshots = await _monitor.GetSnapshots(sites.Values);

            return new {
                websites = snapshots.Select(snapshot => SiteHelper.ToJsonModel(snapshot, sites[snapshot.Name], Context.Request.GetFields()))
            };
        }


        [HttpGet]
        [ResourceInfo(Name = Defines.WebSiteMonitoringName)]
        public async Task<object> Get(string id)
        {
            long siteId = new SiteId(id).Id;

            Site site = ManagementUnit.ServerManager.Sites.FirstOrDefault(s => s.Id == siteId);
            IWebSiteSnapshot snapshot = null;

            if (site != null) {
                snapshot = (await _monitor.GetSnapshots(new Site[] { site })).FirstOrDefault();
            }

            if (snapshot == null) {
                return NotFound();
            }

            return SiteHelper.ToJsonModel(snapshot, site, Context.Request.GetFields());
        }
    }
}
