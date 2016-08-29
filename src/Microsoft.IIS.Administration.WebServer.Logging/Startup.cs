// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Logging
{
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
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.Resource.Guid, $"{Defines.PATH}/{{id?}}", new { controller = "logging" });

            Environment.Hal.ProvideLink(Defines.Resource.Guid, "self", logging => new { href = LoggingHelper.GetLocation(logging.id) });

            // Web Server
            Environment.Hal.ProvideLink(WebServer.Defines.Resource.Guid, Defines.Resource.Name, _ => {
                var id = new LoggingId(null, null, LoggingHelper.IsSectionLocal(null, null));
                return new { href = LoggingHelper.GetLocation(id.Uuid) };
            });

            // Site
            Environment.Hal.ProvideLink(Sites.Defines.Resource.Guid, Defines.Resource.Name, site => {
                var siteId = new SiteId((string)site.id);
                Site s = SiteHelper.GetSite(siteId.Id);
                var id = new LoggingId(siteId.Id, "/", LoggingHelper.IsSectionLocal(s, "/"));
                return new { href = LoggingHelper.GetLocation(id.Uuid) };
            });
        }
    }
}
