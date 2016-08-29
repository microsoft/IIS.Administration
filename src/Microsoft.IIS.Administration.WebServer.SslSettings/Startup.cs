// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.SslSettings
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
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.Resource.Guid, $"{Defines.PATH}/{{id?}}", new { controller = "SslSettings" });

            // Self
            Environment.Hal.ProvideLink(Defines.Resource.Guid, "self", settings => new { href = SslSettingsHelper.GetLocation(settings.id) });

            // Site
            Environment.Hal.ProvideLink(Sites.Defines.Resource.Guid, Defines.Resource.Name, site => {
                var siteId = new SiteId((string)site.id);
                Site s = SiteHelper.GetSite(siteId.Id);
                var id = new SslSettingId(siteId.Id, "/", SslSettingsHelper.IsSectionLocal(s, "/"));
                return new { href = SslSettingsHelper.GetLocation(id.Uuid) };
            });
            
            // Application
            Environment.Hal.ProvideLink(Applications.Defines.Resource.Guid, Defines.Resource.Name, app => {
                var appId = new ApplicationId((string)app.id);
                Site s = SiteHelper.GetSite(appId.SiteId);
                var id = new SslSettingId(appId.SiteId, appId.Path, SslSettingsHelper.IsSectionLocal(s, appId.Path));
                return new { href = SslSettingsHelper.GetLocation(id.Uuid) };
            });
        }
    }
}
