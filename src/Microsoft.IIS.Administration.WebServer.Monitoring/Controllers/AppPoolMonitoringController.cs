// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Monitoring
{
    using Core.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.IIS.Administration.Core;
    using Microsoft.IIS.Administration.WebServer.AppPools;
    using Microsoft.Web.Administration;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    [RequireWebServer]
    public class AppPoolMonitoringController : ApiBaseController
    {
        private IAppPoolMonitor _monitor;

        public AppPoolMonitoringController(IAppPoolMonitor monitor)
        {
            _monitor = monitor;
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.AppPoolMonitoringName)]
        public async Task<object> Get()
        {
            Dictionary<string, ApplicationPool> pools = ManagementUnit.ServerManager.ApplicationPools.ToDictionary(p => p.Name);
            IEnumerable<IAppPoolSnapshot> snapshots = await _monitor.GetSnapshots(pools.Values);

            return new {
                app_pools = snapshots.Select(snapshot => AppPoolHelper.ToJsonModel(snapshot, pools[snapshot.Name], Context.Request.GetFields()))
            };
        }


        [HttpGet]
        [ResourceInfo(Name = Defines.AppPoolMonitoringName)]
        public async Task<object> Get(string id)
        {
            string name = AppPoolId.CreateFromUuid(id).Name;

            ApplicationPool pool = AppPools.AppPoolHelper.GetAppPool(name);
            IAppPoolSnapshot snapshot = null;

            if (pool != null) {
                snapshot = (await _monitor.GetSnapshots(new ApplicationPool[] { pool })).FirstOrDefault();
            }

            if (snapshot == null) {
                return NotFound();
            }

            return AppPoolHelper.ToJsonModel(snapshot, pool, Context.Request.GetFields());
        }
    }
}
