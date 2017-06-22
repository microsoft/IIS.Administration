// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestMonitor
{
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using System.Threading.Tasks;


    [RequireWebServer]
    public class RequestMonitoringController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.MonitorName)]
        [RequireGlobalModule(RequestHelper.MODULE, RequestHelper.DISPLAY_NAME)]
        public object Get()
        {
            return LocationChanged(RequestHelper.GetLocation(), RequestHelper.FeatureToJsonModel());
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.MonitorName)]
        [RequireGlobalModule(RequestHelper.MODULE, RequestHelper.DISPLAY_NAME)]
        public object Get(string id)
        {
            RmId rmId = new RmId();

            if (!rmId.Uuid.Equals(id)) {
                return NotFound();
            }

            return RequestHelper.FeatureToJsonModel();
        }

        [HttpPost]
        [Audit]
        [ResourceInfo(Name = Defines.MonitorName)]
        public async Task<object> Post()
        {
            if (RequestHelper.IsFeatureEnabled()) {
                throw new AlreadyExistsException(RequestHelper.DISPLAY_NAME);
            }

            await RequestHelper.SetFeatureEnabled(true);

            dynamic settings = RequestHelper.FeatureToJsonModel();
            return Created(RequestHelper.GetLocation(), settings);
        }

        [HttpDelete]
        [Audit]
        public async Task Delete(string id)
        {
            RmId rmId = new RmId();

            if (rmId.Uuid.Equals(id) && RequestHelper.IsFeatureEnabled()) {
                await RequestHelper.SetFeatureEnabled(false);
            }
        }
    }
}
