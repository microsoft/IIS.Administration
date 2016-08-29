// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.DirectoryBrowsing
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
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.Resource.Guid, $"{Defines.PATH}/{{id?}}", new { controller = "directorybrowsing" });

            // Self
            Environment.Hal.ProvideLink(Defines.Resource.Guid, "self", dirb => new { href = DirectoryBrowsingHelper.GetLocation(dirb.id) });

            // Webserver
            Environment.Hal.ProvideLink(WebServer.Defines.Resource.Guid, Defines.Resource.Name, _ => {
                var id = GetDirectoryBrowsingId(null, null);
                return new { href = DirectoryBrowsingHelper.GetLocation(id.Uuid) };
            });

            // Site
            Environment.Hal.ProvideLink(Sites.Defines.Resource.Guid, Defines.Resource.Name, site => {
                var siteId = new SiteId((string)site.id);
                Site s = SiteHelper.GetSite(siteId.Id);
                var id = GetDirectoryBrowsingId(s, "/");
                return new { href = DirectoryBrowsingHelper.GetLocation(id.Uuid) };
            });

            // Application
            Environment.Hal.ProvideLink(Applications.Defines.Resource.Guid, Defines.Resource.Name, app => {
                var appId = new ApplicationId((string)app.id);
                Site s = SiteHelper.GetSite(appId.SiteId);
                var id = GetDirectoryBrowsingId(s, appId.Path);
                return new { href = DirectoryBrowsingHelper.GetLocation(id.Uuid) };
            });
        }

        private DirectoryBrowsingId GetDirectoryBrowsingId(Site s, string path)
        {
            return new DirectoryBrowsingId(s?.Id, path, DirectoryBrowsingHelper.IsSectionLocal(s, path));
        }
    }
}
