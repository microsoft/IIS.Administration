// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.WorkerProcesses {
    using AspNetCore.Builder;
    using Core;
    using Core.Http;


    public class Startup : BaseModule {

        public override void Start() {
            ConfigureWorkerProcesses();
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

            // Webserver
            hal.ProvideLink(WebServer.Defines.Resource.Guid, Defines.Resource.Name, _ => {
                return new { href = $"/{Defines.PATH}" };
            });
        }
    }
}
