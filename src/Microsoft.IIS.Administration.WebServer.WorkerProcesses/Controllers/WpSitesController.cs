// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.WorkerProcesses {
    using System.Collections.Generic;
    using System.Linq;
    using AspNetCore.Mvc;
    using Core.Http;
    using Core.Utils;
    using Web.Administration;
    using Core;

    public class WpSitesController : ApiBaseController {

        [HttpGet]
        [ResourceInfo(Name = Sites.Defines.WebsitesName)]
        public object Get() {

            //
            // Filter by WorkerProcess
            string wpUuid = Context.Request.Query[Defines.IDENTIFIER];

            if (string.IsNullOrEmpty(wpUuid)) {
                return NotFound();
            }

            WorkerProcess wp = WorkerProcessHelper.GetWorkerProcess(new WorkerProcessId(wpUuid).Id);

            if (wp == null) {
                return NotFound();
            }

            IEnumerable<Site> sites = SitesHelper.GetSites(wp);

            // 
            // Set HTTP header for total count
            this.Context.Response.SetItemsCount(sites.Count());

            Fields fields = Context.Request.GetFields();

            var obj = new {
                websites = sites.Select(site => Sites.SiteHelper.ToJsonModelRef(site, fields))
            };

            return obj;
        }
    }
}
