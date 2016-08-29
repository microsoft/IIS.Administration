// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.VirtualDirectories
{
    using AspNetCore.Builder;
    using Core;
    using Core.Http;


    public class Startup : BaseModule
    {
        public Startup() { }

        public override void Start()
        {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.Resource.Guid, $"{Defines.PATH}/{{id?}}", new { controller = "virtualdirectories" });

            // Self
            Environment.Hal.ProvideLink(Defines.Resource.Guid, "self", vdir => new { href = VDirHelper.GetLocation(vdir.id) });
            
            // Site
            Environment.Hal.ProvideLink(Sites.Defines.Resource.Guid, Defines.Resource.Name, site => new { href = $"/{Defines.PATH}?{Sites.Defines.IDENTIFIER}={site.id}" });

            // Application
            Environment.Hal.ProvideLink(Applications.Defines.Resource.Guid, Defines.Resource.Name, app => new { href = $"/{Defines.PATH}?{Applications.Defines.IDENTIFIER}={app.id}" });
        }
    }
}
