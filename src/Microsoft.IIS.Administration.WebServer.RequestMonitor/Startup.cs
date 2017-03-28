// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestMonitor {
    using AspNetCore.Builder;
    using Core;
    using Core.Http;

    public class Startup : BaseModule {

        public override void Start() {
            ConfigureRequestMonitoring();
            ConfigureRequests();
        }

        private void ConfigureRequestMonitoring()
        {
            //
            // Controller
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.Resource.Guid, $"{Defines.PATH}/{{id?}}", new { controller = "RequestMonitoring" });

            //
            // Hal
            var hal = Environment.Hal;

            // Self
            hal.ProvideLink(Defines.Resource.Guid, "self", r => new { href = $"/{Defines.PATH}/{r.id}" });

            // Webserver
            hal.ProvideLink(WebServer.Defines.Resource.Guid, Defines.Resource.Name, _ => {
                return new { href = RequestHelper.GetLocation() };
            });
        }

        private void ConfigureRequests() {
            //
            // Controller
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.RequestsResource.Guid, $"{Defines.REQUESTS_PATH}/{{id?}}", new { controller = "RmRequests" });

            //
            // Hal
            var hal = Environment.Hal;

            // Self
            hal.ProvideLink(Defines.RequestsResource.Guid, "self", r => new { href = $"/{Defines.REQUESTS_PATH}/{r.id}" });

            // WorkerProcess
            hal.ProvideLink(WorkerProcesses.Defines.Resource.Guid, Defines.RequestsResource.Name, wp => {
                return new { href = $"/{Defines.REQUESTS_PATH}?{Defines.WP_IDENTIFIER}={wp.id}" };
            });

            // Site
            hal.ProvideLink(Sites.Defines.Resource.Guid, Defines.RequestsResource.Name, site => {
                return new { href = $"/{Defines.REQUESTS_PATH}?{Sites.Defines.IDENTIFIER}={site.id}" };
            });

            // Feature
            hal.ProvideLink(Defines.Resource.Guid, Defines.RequestsResource.Name, _ => new { href = $"/{Defines.REQUESTS_PATH}" });
        }
    }
}
