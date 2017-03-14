// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRequestTracing
{
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Files;
    using Sites;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Web.Administration;

    [RequireGlobalModule(Helper.TRACING_MODULE, Helper.DISPLAY_NAME)]
    [RequireGlobalModule(Helper.FAILED_REQUEST_TRACING_MODULE, Helper.DISPLAY_NAME)]
    public class RequestTracesController : ApiBaseController
    {
        private IFileProvider _provider;

        public RequestTracesController(IFileProvider provider)
        {
            _provider = provider;
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.TracesName)]
        public async Task<object> Get()
        {
            string hrtUuid = Context.Request.Query[Defines.IDENTIFIER];

            if (string.IsNullOrEmpty(hrtUuid)) {
                return NotFound();
            }

            var id = new HttpRequestTracingId(hrtUuid);
            Site site = id?.SiteId == null ? null : SiteHelper.GetSite(id.SiteId.Value);

            if (site == null) {
                return NotFound();
            }

            var helper = new TracesHelper(_provider, site);

            IEnumerable<TraceInfo> traces = await helper.GetTraces();

            Context.Response.SetItemsCount(traces.Count());

            return new {
                traces = traces.Select(t => helper.ToJsonModel(t, Context.Request.GetFields()))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.TraceName)]
        public async Task<object> Get(string id)
        {
            TraceId traceId = new TraceId(id);
            Site site = SiteHelper.GetSite(traceId.SiteId);

            if (site == null) {
                return NotFound();
            }

            var helper = new TracesHelper(_provider, site);

            TraceInfo trace = await helper.GetTrace(traceId.Name);

            if (trace == null) {
                return NotFound();
            }

            return helper.ToJsonModel(trace, Context.Request.GetFields());
        }
    }
}
