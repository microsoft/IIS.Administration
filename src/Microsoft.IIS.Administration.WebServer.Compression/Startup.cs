// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Compression
{
    using Applications;
    using AspNetCore.Builder;
    using Core;
    using Core.Http;
    using Sites;
    using Web.Administration;


    public class Startup : BaseModule
    {
        public Startup() { }

        public override void Start()
        {
            var host = Environment.Host;
            var hal = Environment.Hal;

            host.RouteBuilder.MapWebApiRoute(Defines.Resource.Guid, $"{Defines.PATH}/{{id?}}", new { controller = "compression" });

            // Self
            hal.ProvideLink(Defines.Resource.Guid, "self", comp => new { href = CompressionHelper.GetLocation(comp.id) });

            // Web Server
            hal.ProvideLink(WebServer.Defines.Resource.Guid, Defines.Resource.Name, _ => {
                var id = new CompressionId(null, null, CompressionHelper.IsSectionLocal(null, null));
                return new { href = CompressionHelper.GetLocation(id.Uuid) };
            });

            // Site
            hal.ProvideLink(Sites.Defines.Resource.Guid, Defines.Resource.Name, site => {
                var siteId = new SiteId((string)site.id);
                Site s = SiteHelper.GetSite(siteId.Id);
                var id = new CompressionId(siteId.Id, "/", CompressionHelper.IsSectionLocal(s, "/"));
                return new { href = CompressionHelper.GetLocation(id.Uuid) };
            });

            // Application
            hal.ProvideLink(Applications.Defines.Resource.Guid, Defines.Resource.Name, app => {
                var appId = new ApplicationId((string)app.id);
                Site s = SiteHelper.GetSite(appId.SiteId);
                var id = new CompressionId(appId.SiteId, appId.Path, CompressionHelper.IsSectionLocal(s, appId.Path));
                return new { href = CompressionHelper.GetLocation(id.Uuid) };
            });
        }
    }
}
