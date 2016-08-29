// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Handlers
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
            ConfigureMappings();
            ConfigureHandlers();
        }

        private void ConfigureMappings()
        {
            // Provide mvc with route
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.MappingsResource.Guid, $"{ Defines.MAPPINGS_PATH}/{{id?}}", new { controller = "handlermappings" });

            // Register self hypermedia
            Environment.Hal.ProvideLink(Defines.MappingsResource.Guid, "self", mapping => new { href = MappingsHelper.GetLocation(mapping.id) });

            // Provide link to mappings sub resource
            Environment.Hal.ProvideLink(Defines.Resource.Guid, "entries", mapping => new { href = $"/{Defines.MAPPINGS_PATH}?{Defines.IDENTIFIER}={mapping.id}" });
        }

        private void ConfigureHandlers()
        {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.Resource.Guid, $"{Defines.PATH}/{{id?}}", new { controller = "handlers" });

            Environment.Hal.ProvideLink(Defines.Resource.Guid, "self", handlers => new { href = HandlersHelper.GetLocation(handlers.id) });

            // Web Server
            Environment.Hal.ProvideLink(WebServer.Defines.Resource.Guid, Defines.Resource.Name, _ => {
                var id = GetHandlersId(null, null);
                return new { href = HandlersHelper.GetLocation(id.Uuid) };
            });

            // Site
            Environment.Hal.ProvideLink(Sites.Defines.Resource.Guid, Defines.Resource.Name, site => {
                var siteId = new SiteId((string)site.id);
                Site s = SiteHelper.GetSite(siteId.Id);
                var id = GetHandlersId(s, "/");
                return new { href = HandlersHelper.GetLocation(id.Uuid) };
            });

            // Application
            Environment.Hal.ProvideLink(Applications.Defines.Resource.Guid, Defines.Resource.Name, app => {
                var appId = new ApplicationId((string)app.id);
                Site s = SiteHelper.GetSite(appId.SiteId);
                var id = GetHandlersId(s, appId.Path);
                return new { href = HandlersHelper.GetLocation(id.Uuid) };
            });
        }

        private HandlersId GetHandlersId(Site s, string path)
        {
            return new HandlersId(s?.Id, path, HandlersHelper.IsSectionLocal(s, path));
        }
    }
}
