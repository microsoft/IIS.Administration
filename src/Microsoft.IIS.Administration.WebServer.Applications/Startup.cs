// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Applications {
    using AspNetCore.Builder;
    using Core;
    using Core.Http;

    public class Startup : BaseModule
    {
        public Startup() { }

        public override void Start() {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.Resource.Guid, $"{Defines.PATH}/{{id?}}", new { controller = "applications" });

            var hal = Environment.Hal;

            hal.ProvideLink(Defines.Resource.Guid, "self", app => new { href = ApplicationHelper.GetLocation(app.id) });
            hal.ProvideLink(Sites.Defines.Resource.Guid, Defines.Resource.Name, site => new { href = $"/{Defines.PATH}?{Sites.Defines.IDENTIFIER}={site.id}" });
            hal.ProvideLink(AppPools.Defines.Resource.Guid, Defines.Resource.Name, pool => new { href = $"/{Defines.PATH}?{AppPools.Defines.IDENTIFIER}={pool.id}" });
        }
    }
}
