// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Delegation
{
    using AspNetCore.Builder;
    using Core;
    using Core.Http;


    public class Startup : BaseModule
    {
        public Startup() { }

        public override void Start()
        {
            // Provide MVC with route
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.Resource.Guid, $"{Defines.PATH}/{{id?}}", new { controller = "delegation" });

            // Self
            Environment.Hal.ProvideLink(Defines.Resource.Guid, "self", section => new { href = $"/{Defines.PATH}/{section.id}" });

            // Web Server
            Environment.Hal.ProvideLink(WebServer.Defines.Resource.Guid, Defines.Resource.Name, _ => new { href = $"/{Defines.PATH}" });
            
            // Site
            Environment.Hal.ProvideLink(Sites.Defines.Resource.Guid, Defines.Resource.Name, site => new { href = $"/{Defines.PATH}?{Sites.Defines.IDENTIFIER}={site.id}" });
        }
    }
}
