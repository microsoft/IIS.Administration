// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.WorkerProcesses {
    using AspNetCore.Mvc;
    using System.Linq;
    using System.Net;
    using Core.Http;
    using System.Collections.Generic;
    using Web.Administration;
    using AppPools;
    using Core.Utils;
    using Core;

    [RequireWebServer]
    public class WorkerProcessesController : ApiBaseController {

        [HttpGet]
        [ResourceInfo(Name = Defines.WorkerProcessesName)]
        public object Get() {

            IEnumerable<WorkerProcess> wps = null;

            //
            // Filter by AppPool
            string appPoolUuid = Context.Request.Query[AppPools.Defines.IDENTIFIER];

            if (!string.IsNullOrEmpty(appPoolUuid)) {
                ApplicationPool pool = AppPoolHelper.GetAppPool(AppPoolId.CreateFromUuid(appPoolUuid).Name);

                if (pool == null) {
                    return NotFound();
                }

                wps = WorkerProcessHelper.GetWorkerProcesses(pool);
            }

            //
            // All
            if (wps == null) {
                wps = WorkerProcessHelper.GetWorkerProcesses();
            }

            // 
            // Set HTTP header for total count
            this.Context.Response.SetItemsCount(wps.Count());

            Fields fields = Context.Request.GetFields();

            var obj = new {
                worker_processes = wps.Select(wp => WorkerProcessHelper.ToJsonModelRef(wp, fields))
            };

            return obj;
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.WorkerProcessName)]
        public object Get(string id)
        {
            var target = WorkerProcessHelper.GetWorkerProcess(new WorkerProcessId(id).Id);

            if (target == null) {
                return NotFound();
            }

            return WorkerProcessHelper.WpToJsonModel(target, Context.Request.GetFields());
        }

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            var target = WorkerProcessHelper.GetWorkerProcess(new WorkerProcessId(id).Id);

            if (target != null) {
                WorkerProcessHelper.Kill(target);
            }

            // Success
            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }
    }
}
