// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Sites
{
    using AspNetCore.Builder;
    using Core;
    using Core.Http;

    public class Startup : BaseModule
    {
        public Startup() { }

        public override void Start()
        {   
            // Provide mvc with route for sites requests
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.Resource.Guid, $"{Defines.PATH}/{{id?}}", new { controller = "sites" });

            //
            // Hal
            var hal = Environment.Hal;

            // Register self hypermedia
            hal.ProvideLink(Defines.Resource.Guid, "self", site => new { href = SiteHelper.GetLocation(site.id) });

            // Provide hypermedia for other plugins
            hal.ProvideLink(WebServer.Defines.Resource.Guid, Defines.Resource.Name, _ => new { href = $"/{Defines.PATH}" });
            hal.ProvideLink(AppPools.Defines.Resource.Guid, Defines.Resource.Name, pool => new { href = $"/{Defines.PATH}?{AppPools.Defines.IDENTIFIER}={pool.id}" });

            // Mark appropriate website fields as nonsensitive for resources that use site references
            INonsensitiveAuditingFields nonsensitiveFields = (INonsensitiveAuditingFields)Environment.Host.ApplicationBuilder.ApplicationServices.GetService(typeof(INonsensitiveAuditingFields));
            if (nonsensitiveFields != null) {
                nonsensitiveFields.Add("website.key");
            }
        }
    }
}
