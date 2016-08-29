// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.WorkerProcesses {
    using AspNetCore.Builder;
    using Core;
    using Core.Http;


    public class Startup : BaseModule {

        public override void Start() {
            ConfigureSites();
            ConfigureApplications();
            ConfigureWorkerProcesses();
        }

        private void ConfigureSites() {
            //
            // Controller
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.SitesResource.Guid, $"{Defines.SITES_PATH}", new { controller = "WpSites" });

            //
            // Hal
            var hal = Environment.Hal;

            // WorkerProcess
            hal.ProvideLink(Defines.Resource.Guid, Defines.SitesResource.Name, wp => {
                return new { href = $"/{Defines.SITES_PATH}?{Defines.IDENTIFIER}={wp.id}" };
            });
        }

        private void ConfigureApplications() {
            //
            // Controller
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.AppsResource.Guid, $"{Defines.APPS_PATH}", new { controller = "WpApplications" });

            //
            // Hal
            var hal = Environment.Hal;

            // WorkerProcess
            hal.ProvideLink(Defines.Resource.Guid, Defines.AppsResource.Name, wp => {
                return new { href = $"/{Defines.APPS_PATH}?{Defines.IDENTIFIER}={wp.id}" };
            });
        }

        private void ConfigureWorkerProcesses() {
            //
            // Controller
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.Resource.Guid, $"{Defines.PATH}/{{id?}}", new { controller = "WorkerProcesses" });


            //
            // Hal
            var hal = Environment.Hal;

            // Self
            hal.ProvideLink(Defines.Resource.Guid, "self", wp => new { href = $"/{Defines.PATH}/{wp.id}" });

            // AppPool
            hal.ProvideLink(AppPools.Defines.Resource.Guid, Defines.Resource.Name, pool => {
                return new { href = $"/{Defines.PATH}?{AppPools.Defines.IDENTIFIER}={pool.id}" };
            });

            // Application
            hal.ProvideLink(Applications.Defines.Resource.Guid, Defines.Resource.Name, app => {
                return new { href = $"/{Defines.PATH}?{Applications.Defines.IDENTIFIER}={app.id}" };
            });

            // Site
            hal.ProvideLink(Sites.Defines.Resource.Guid, Defines.Resource.Name, site => {
                return new { href = $"/{Defines.PATH}?{Sites.Defines.IDENTIFIER}={site.id}" };
            });

            // Webserver
            hal.ProvideLink(WebServer.Defines.Resource.Guid, Defines.Resource.Name, _ => {
                return new { href = $"/{Defines.PATH}" };
            });
        }
    }
}
