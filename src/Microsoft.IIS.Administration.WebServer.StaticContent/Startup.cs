// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.StaticContent
{
    using Applications;
    using AspNetCore.Builder;
    using Core;
    using Core.Http;
    using Sites;
    using Web.Administration;


    public class Startup : BaseModule
    {
        public override void Start()
        {
            ConfigureMimeMaps();
            ConfigureStaticContent();
        }

        private void ConfigureStaticContent()
        {
            // Provide mvc with route
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.Resource.Guid, $"{Defines.PATH}/{{id?}}", new { controller = "staticcontent" });

            // Register self hypermedia
            Environment.Hal.ProvideLink(Defines.Resource.Guid, "self", sc => new { href = StaticContentHelper.GetLocation(sc.id) });

            // Web Server
            Environment.Hal.ProvideLink(WebServer.Defines.Resource.Guid, Defines.Resource.Name, _ => {
                var id = GetStaticContentId(null, null);
                return new { href = StaticContentHelper.GetLocation(id.Uuid) };
            });

            // Site
            Environment.Hal.ProvideLink(Sites.Defines.Resource.Guid, Defines.Resource.Name, site => {
                var siteId = new SiteId((string)site.id);
                Site s = SiteHelper.GetSite(siteId.Id);
                var id = GetStaticContentId(s, "/");
                return new { href = StaticContentHelper.GetLocation(id.Uuid) };
            });

            // Application
            Environment.Hal.ProvideLink(Applications.Defines.Resource.Guid, Defines.Resource.Name, app => {
                var appId = new ApplicationId((string)app.id);
                Site s = SiteHelper.GetSite(appId.SiteId);
                var id = GetStaticContentId(s, appId.Path);
                return new { href = StaticContentHelper.GetLocation(id.Uuid) };
            });
        }

        private void ConfigureMimeMaps()
        {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.MimeMapsResource.Guid, $"{ Defines.MIME_MAPS_PATH}/{{id?}}", new { controller = "mimemaps" });

            Environment.Hal.ProvideLink(Defines.MimeMapsResource.Guid, "self", mimeMap => new { href = $"/{Defines.MIME_MAPS_PATH}/{mimeMap.id}" });

            Environment.Hal.ProvideLink(Defines.Resource.Guid, Defines.MimeMapsResource.Name, sc => new { href = $"/{Defines.MIME_MAPS_PATH}?{Defines.IDENTIFIER}={sc.id}" });
        }

        private StaticContentId GetStaticContentId(Site s, string path)
        {
            return new StaticContentId(s?.Id, path, StaticContentHelper.IsSectionLocal(s, path));
        }
    }
}
