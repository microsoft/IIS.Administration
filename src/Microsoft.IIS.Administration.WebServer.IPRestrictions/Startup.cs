// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.IPRestrictions
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
            ConfigureRules();
            ConfigureIPRestrictions();
        }

        private void ConfigureIPRestrictions()
        {
            // Register controller routes in mvc framework
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.Resource.Guid, $"{Defines.PATH}/{{id?}}", new { controller = "iprestriction" });

            // Self
            Environment.Hal.ProvideLink(Defines.Resource.Guid, "self", ipRes => new { href = IPRestrictionsHelper.GetLocation(ipRes.id) });

            // Web Server
            Environment.Hal.ProvideLink(WebServer.Defines.Resource.Guid, Defines.Resource.Name, _ => {
                var id = GetIPRestrictionId(null, null);
                return new { href = IPRestrictionsHelper.GetLocation(id.Uuid) };
            });

            // Site
            Environment.Hal.ProvideLink(Sites.Defines.Resource.Guid, Defines.Resource.Name, site => {
                var siteId = new SiteId((string)site.id);
                Site s = SiteHelper.GetSite(siteId.Id);
                var id = GetIPRestrictionId(s, "/");
                return new { href = IPRestrictionsHelper.GetLocation(id.Uuid) };
            });

            // Application
            Environment.Hal.ProvideLink(Applications.Defines.Resource.Guid, Defines.Resource.Name, app => {
                var appId = new ApplicationId((string)app.id);
                Site s = SiteHelper.GetSite(appId.SiteId);
                var id = GetIPRestrictionId(s, appId.Path);
                return new { href = IPRestrictionsHelper.GetLocation(id.Uuid) };
            });
        }

        private void ConfigureRules()
        {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.RulesResource.Guid, $"{Defines.RULES_PATH}/{{id?}}", new { controller = "iprestrictionrules" });

            // Provide self links for plugin resources
            Environment.Hal.ProvideLink(Defines.RulesResource.Guid, "self", rule => new { href = IPRestrictionsHelper.GetRuleLocation(rule.id) });

            // Provide link for the rules sub resource
            Environment.Hal.ProvideLink(Defines.Resource.Guid, "entries", ipRes => new { href = $"/{Defines.RULES_PATH}?{Defines.IDENTIFIER}={ipRes.id}" });
        }

        private IPRestrictionId GetIPRestrictionId(Site s, string path)
        {
            return new IPRestrictionId(s?.Id, path, IPRestrictionsHelper.IsSectionLocal(s, path));
        }
    }
}
