// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Monitoring
{
    using Core.Http;
    using Microsoft.IIS.Administration.Core;
    using System.Threading.Tasks;

    [RequireWebServer]
    public class WebServerMonitoringController : ApiBaseController
    {
        private IWebServerMonitor _monitor;

        public WebServerMonitoringController(IWebServerMonitor monitor)
        {
            _monitor = monitor;
        }

        [ResourceInfo(Name = Defines.WebServerMonitoringName)]
        public async Task<object> Get()
        {
            return WebServerHelper.ToJsonModel(await _monitor.GetSnapshot(), Context.Request.GetFields());
        }
    }
}
