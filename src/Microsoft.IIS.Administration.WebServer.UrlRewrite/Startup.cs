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
            ConfigureOutboundRules();
        }

        private void ConfigureUrlRewrite()
        {
            // MVC routing
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.Resource.Guid, $"{Defines.PATH}/{{id?}}", new { controller = "UrlRewrite" });

            // Self
            Environment.Hal.ProvideLink(Defines.Resource.Guid, "self", rf => new { href = RewriteHelper.GetLocation(rf.id) });


            // Web Server
            Environment.Hal.ProvideLink(WebServer.Defines.Resource.Guid, Defines.Resource.Name, _ => {
                var id = GetRewriteId(null, null);
                return new { href = RewriteHelper.GetLocation(id.Uuid) };
            });

            // Site
            Environment.Hal.ProvideLink(Sites.Defines.Resource.Guid, Defines.Resource.Name, site => {
                var siteId = new SiteId((string)site.id);
                Site s = SiteHelper.GetSite(siteId.Id);
                var id = GetRewriteId(s, "/");
                return new { href = RewriteHelper.GetLocation(id.Uuid) };
            });

            // Application
            Environment.Hal.ProvideLink(Applications.Defines.Resource.Guid, Defines.Resource.Name, app => {
                var appId = new ApplicationId((string)app.id);
                Site s = SiteHelper.GetSite(appId.SiteId);
                var id = GetRewriteId(s, appId.Path);
                return new { href = RewriteHelper.GetLocation(id.Uuid) };
            });
        }

        private void ConfigureServerVariables()
        {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.ServerVariablesResource.Guid, $"{Defines.SERVER_VARIABLES_PATH}/{{id?}}", new { controller = "ServerVariables" });

            // Server Variables -> Self
            Environment.Hal.ProvideLink(Defines.ServerVariablesResource.Guid, "self", sv => new { href = ServerVariablesHelper.GetLocation(sv.id) });

            // Rewrite -> Server Variables
            Environment.Hal.ProvideLink(Defines.Resource.Guid, Defines.ServerVariablesResource.Name, rewrite => new { href = ServerVariablesHelper.GetLocation(rewrite.id) });
        }

        private void ConfigureInboundRules()
        {
            var builder = Environment.Host.RouteBuilder;
            var hal = Environment.Hal;

            builder.MapWebApiRoute(Defines.InboundRulesSectionResource.Guid, $"{Defines.INBOUND_RULES_SECTION_PATH}/{{id?}}", new { controller = "InboundRulesSection" });
            builder.MapWebApiRoute(Defines.InboundRulesResource.Guid, $"{Defines.INBOUND_RULES_PATH}/{{id?}}", new { controller = "InboundRules" });

            hal.ProvideLink(Defines.InboundRulesSectionResource.Guid, "self", ir => new { href = InboundRulesHelper.GetSectionLocation(ir.id) });
            hal.ProvideLink(Defines.InboundRulesResource.Guid, "self", ir => new { href = InboundRulesHelper.GetRuleLocation(ir.id) });

            // Rewrite -> Section
            hal.ProvideLink(Defines.Resource.Guid, Defines.InboundRulesSectionResource.Name, rewrite => new { href = InboundRulesHelper.GetSectionLocation(rewrite.id) });

            // Section -> Rules
            hal.ProvideLink(Defines.InboundRulesSectionResource.Guid, Defines.InboundRulesResource.Name, inboundRulesSection =>  new { href = $"{Defines.INBOUND_RULES_PATH}?{Defines.INBOUND_RULES_SECTION_IDENTIFIER}={inboundRulesSection.id}" });
        }

        private void ConfigureOutboundRules()
        {
            var builder = Environment.Host.RouteBuilder;
            var hal = Environment.Hal;

            builder.MapWebApiRoute(Defines.OutboundRulesSectionResource.Guid, $"{Defines.OUTBOUND_RULES_SECTION_PATH}/{{id?}}", new { controller = "OutboundRulesSection" });
            builder.MapWebApiRoute(Defines.OutboundRulesResource.Guid, $"{Defines.OUTBOUND_RULES_PATH}/{{id?}}", new { controller = "OutboundRules" });
            builder.MapWebApiRoute(Defines.PreConditionsResource.Guid, $"{Defines.PRECONDITIONS_PATH}/{{id?}}", new { controller = "PreConditions" });

            hal.ProvideLink(Defines.OutboundRulesSectionResource.Guid, "self", ir => new { href = OutboundRulesHelper.GetSectionLocation(ir.id) });
            hal.ProvideLink(Defines.OutboundRulesResource.Guid, "self", ir => new { href = OutboundRulesHelper.GetRuleLocation(ir.id) });
            hal.ProvideLink(Defines.PreConditionsResource.Guid, "self", pc => new { href = OutboundRulesHelper.GetPreConditionLocation(pc.id) });

            // Rewrite -> Section
            hal.ProvideLink(Defines.Resource.Guid, Defines.OutboundRulesSectionResource.Name, rewrite => new { href = OutboundRulesHelper.GetSectionLocation(rewrite.id) });

            // Section -> Rules
            hal.ProvideLink(Defines.OutboundRulesSectionResource.Guid, Defines.OutboundRulesResource.Name, outboundRulesSection => new { href = $"{Defines.OUTBOUND_RULES_PATH}?{Defines.OUTBOUND_RULES_SECTION_IDENTIFIER}={outboundRulesSection.id}" });
            
            // Section -> PreConditions
            hal.ProvideLink(Defines.OutboundRulesSectionResource.Guid, Defines.PreConditionsResource.Name, outboundRulesSection => new { href = $"{Defines.PRECONDITIONS_PATH}?{Defines.OUTBOUND_RULES_SECTION_IDENTIFIER}={outboundRulesSection.id}" });
        }

        private RewriteId GetRewriteId(Site s, string path)
        {
            return new RewriteId(s?.Id, path);
        }
    }
}

