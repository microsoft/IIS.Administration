// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.WorkerProcesses {
    using AspNetCore.Mvc;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using Core.Http;
    using System.Collections.Generic;
    using Web.Administration;
    using AppPools;
    using Core.Utils;
    using Applications;
    using Core;

    public class WpApplicationsController : ApiBaseController {

        [HttpGet]
        [ResourceInfo(Name = Applications.Defines.WebAppsName)]
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

            IEnumerable<ApplicationInfo> apps = ApplicationHelper.GetApplications(wp);

            // 
            // Set HTTP header for total count
            this.Context.Response.SetItemsCount(apps.Count());

            Fields fields = Context.Request.GetFields();

            var obj = new {
                webapps = apps.Select(app => fields.HasFields ? Applications.ApplicationHelper.ToJsonModel(app.Application, app.Site, fields) :
                                                                Applications.ApplicationHelper.ToJsonModelRef(app.Application, app.Site))
            };

            return obj;
        }
    }
}
