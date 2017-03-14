// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRedirect
{
    using Applications;
    using AspNetCore.Builder;
    using Core;
    using Sites;
    using Web.Administration;
    using Core.Http;

    public class Startup : BaseModule
    {
        public Startup() { }

        public override void Start()
        {
            var host = Environment.Host;
            // Register all controller routes in mvc framework
            host.RouteBuilder.MapWebApiRoute(Defines.Resource.Guid, $"{Defines.PATH}/{{id?}}", new { controller = "HttpRedirect" });

            // Self
            Environment.Hal.ProvideLink(Defines.Resource.Guid, "self", doc => new { href = RedirectHelper.GetLocation(doc.id) });

            // Web Server
            Environment.Hal.ProvideLink(WebServer.Defines.Resource.Guid, Defines.Resource.Name, _ => {
                var id = GetDefaultDocumentId(null, null);
                return new { href = RedirectHelper.GetLocation(id.Uuid) };
            });

            // Site
            Environment.Hal.ProvideLink(Sites.Defines.Resource.Guid, Defines.Resource.Name, site => {
                var siteId = new SiteId((string)site.id);
                Site s = SiteHelper.GetSite(siteId.Id);
                var id = GetDefaultDocumentId(s, "/");
                return new { href = RedirectHelper.GetLocation(id.Uuid) };
            });

            // Application
            Environment.Hal.ProvideLink(Applications.Defines.Resource.Guid, Defines.Resource.Name, app => {
                var appId = new ApplicationId((string)app.id);
                Site s = SiteHelper.GetSite(appId.SiteId);
                var id = GetDefaultDocumentId(s, appId.Path);
                return new { href = RedirectHelper.GetLocation(id.Uuid) };
            });
        }

        private RedirectId GetDefaultDocumentId(Site site, string path)
        {
            return new RedirectId(site?.Id, path, RedirectHelper.IsSectionLocal(site, path));
        }
    }
}
