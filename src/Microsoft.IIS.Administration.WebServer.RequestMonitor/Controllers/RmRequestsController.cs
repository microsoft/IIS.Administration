// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestMonitor {
    using AspNetCore.Mvc;
    using System.Linq;
    using Core.Http;
    using System.Collections.Generic;
    using Web.Administration;
    using Core.Utils;
    using WorkerProcesses;
    using Core;
    using Sites;

    [RequireGlobalModule(RequestHelper.MODULE, RequestHelper.DISPLAY_NAME)]
    public class RmRequestsController : ApiBaseController {

        [HttpGet]
        [ResourceInfo(Name = Defines.RequestsName)]
        public object Get() {

            IEnumerable<Request> requests = null;

            //
            // Filter by WorkerProcess
            string wpUuid = Context.Request.Query[Defines.WP_IDENTIFIER];

            if (!string.IsNullOrEmpty(wpUuid)) {
                WorkerProcess wp = WorkerProcessHelper.GetWorkerProcess(new WorkerProcessId(wpUuid).Id);
                if (wp == null) {
                    return NotFound();
                }

                requests = RequestHelper.GetRequests(wp, Context.Request.GetFilter());
            }

            //
            // Filter by Site
            if (requests == null) {
                string siteUuid = Context.Request.Query[Sites.Defines.IDENTIFIER];

                if (!string.IsNullOrEmpty(siteUuid)) {
                    Site site = SiteHelper.GetSite(new SiteId(siteUuid).Id);

                    if (site == null) {
                        return NotFound();
                    }

                    requests = RequestHelper.GetRequests(site, Context.Request.GetFilter());
                }
            }

            //
            // Get all
            if (requests == null) {
                requests = RequestHelper.GetRequests(Context.Request.GetFilter());
            }

            // 
            // Set HTTP header for total count
            this.Context.Response.SetItemsCount(requests.Count());

            Fields fields = Context.Request.GetFields();

            return new {
                requests = requests.Select(r => RequestHelper.ToJsonModelRef(r, fields))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.RequestName)]
        public object Get(string id) {
            var reqId = new RequestId(id);

            WorkerProcess wp = WorkerProcessHelper.GetWorkerProcess(reqId.ProcessId);

            if (wp == null) {
                return NotFound();
            }

            Request request = RequestHelper.GetRequest(wp, reqId.Id);

            if (request == null) {
                return NotFound();
            }

            return RequestHelper.ToJsonModel(request, Context.Request.GetFields());
        }
    }
}
