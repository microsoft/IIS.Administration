// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
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
            ConfigureUrlRewrite();
            ConfigureServerVariables();
            ConfigureInboundRules();
            ConfigureInboundRuleEntries();
        }

        private void ConfigureUrlRewrite()
        {
            // MVC routing
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.Resource.Guid, $"{Defines.PATH}/{{id?}}", new { controller = "UrlRewrite" });

            // Self
            Environment.Hal.ProvideLink(Defines.Resource.Guid, "self", rf => new { href = RewriteHelper.GetLocation(rf.id) });


            // Web Server
            Environment.Hal.ProvideLink(WebServer.Defines.Resource.Guid, Defines.Resource.Name, _ => {
                var id = GetRequestFilteringId(null, null);
                return new { href = RewriteHelper.GetLocation(id.Uuid) };
            });

            // Site
            Environment.Hal.ProvideLink(Sites.Defines.Resource.Guid, Defines.Resource.Name, site => {
                var siteId = new SiteId((string)site.id);
                Site s = SiteHelper.GetSite(siteId.Id);
                var id = GetRequestFilteringId(s, "/");
                return new { href = RewriteHelper.GetLocation(id.Uuid) };
            });

            // Application
            Environment.Hal.ProvideLink(Applications.Defines.Resource.Guid, Defines.Resource.Name, app => {
                var appId = new ApplicationId((string)app.id);
                Site s = SiteHelper.GetSite(appId.SiteId);
                var id = GetRequestFilteringId(s, appId.Path);
                return new { href = RewriteHelper.GetLocation(id.Uuid) };
            });
        }

        private void ConfigureServerVariables()
        {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.ServerVariablesResource.Guid, $"{Defines.SERVER_VARIABLES_PATH}/{{id?}}", new { controller = "ServerVariables" });

            // Self
            Environment.Hal.ProvideLink(Defines.ServerVariablesResource.Guid, "self", sv => new { href = ServerVariablesHelper.GetLocation(sv.id) });

            // Rewrite
            Environment.Hal.ProvideLink(Defines.Resource.Guid, Defines.ServerVariablesResource.Name, rewrite => {
                var rewriteId = new RewriteId((string)rewrite.id);
                var id = new ServerVariablesId(rewriteId.SiteId, rewriteId.Path);
                return new { href = ServerVariablesHelper.GetLocation(id.Uuid) };
            });
        }

        private void ConfigureInboundRules()
        {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.InboundRulesSectionResource.Guid, $"{Defines.INBOUND_RULES_SECTION_PATH}/{{id?}}", new { controller = "InboundRulesSection" });

            // Self
            Environment.Hal.ProvideLink(Defines.InboundRulesSectionResource.Guid, "self", ir => new { href = InboundRulesHelper.GetSectionLocation(ir.id) });

            // Rewrite
            Environment.Hal.ProvideLink(Defines.Resource.Guid, Defines.InboundRulesSectionResource.Name, rewrite => {
                var rewriteId = new RewriteId((string)rewrite.id);
                var id = new InboundRulesSectionId(rewriteId.SiteId, rewriteId.Path);
                return new { href = InboundRulesHelper.GetSectionLocation(id.Uuid) };
            });
        }

        private void ConfigureInboundRuleEntries()
        {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.InboundRulesResource.Guid, $"{Defines.INBOUND_RULES_PATH}/{{id?}}", new { controller = "InboundRules" });

            // Self
            Environment.Hal.ProvideLink(Defines.InboundRulesResource.Guid, "self", ir => new { href = InboundRulesHelper.GetRuleLocation(ir.id) });

            // Section
            Environment.Hal.ProvideLink(Defines.InboundRulesSectionResource.Guid, Defines.InboundRulesResource.Name, inboundRulesSection => {
                var inboundRulesId = new InboundRulesSectionId((string)inboundRulesSection.id);
                return new { href = $"{Defines.INBOUND_RULES_PATH}?{Defines.INBOUND_RULES_SECTION_IDENTIFIER}={inboundRulesId.Uuid}" };
            });
        }

        private RewriteId GetRequestFilteringId(Site s, string path)
        {
            return new RewriteId(s?.Id, path);
        }
    }
}

