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
            ConfigureProviders();
            ConfigureRewriteMaps();
            ConfigureGlobalRules();
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

        private void ConfigureRewriteMaps()
        {
            var builder = Environment.Host.RouteBuilder;
            var hal = Environment.Hal;

            builder.MapWebApiRoute(Defines.RewriteMapsSectionResource.Guid, $"{Defines.REWRITE_MAPS_SECTION_PATH}/{{id?}}", new { controller = "RewriteMapsSection" });
            builder.MapWebApiRoute(Defines.RewriteMapsResource.Guid, $"{Defines.REWRITE_MAPS_PATH}/{{id?}}", new { controller = "RewriteMaps" });

            // () -> Self
            hal.ProvideLink(Defines.RewriteMapsSectionResource.Guid, "self", sv => new { href = RewriteMapsHelper.GetSectionLocation(sv.id) });
            hal.ProvideLink(Defines.RewriteMapsResource.Guid, "self", ir => new { href = RewriteMapsHelper.GetMapLocation(ir.id) });

            // Rewrite -> Section
            hal.ProvideLink(Defines.Resource.Guid, Defines.RewriteMapsSectionResource.Name, rewrite => new { href = RewriteMapsHelper.GetSectionLocation(rewrite.id) });

            // Section -> Maps
            hal.ProvideLink(Defines.RewriteMapsSectionResource.Guid, Defines.RewriteMapsResource.Name, section => new { href = $"/{Defines.REWRITE_MAPS_PATH}?{Defines.IDENTIFIER}={section.id}" });
        }

        private void ConfigureProviders()
        {
            var builder = Environment.Host.RouteBuilder;
            var hal = Environment.Hal;

            builder.MapWebApiRoute(Defines.ProvidersSectionResource.Guid, $"{Defines.PROVIDERS_SECTION_PATH}/{{id?}}", new { controller = "ProvidersSection" });
            builder.MapWebApiRoute(Defines.ProvidersResource.Guid, $"{Defines.PROVIDERS_PATH}/{{id?}}", new { controller = "Providers" });

            hal.ProvideLink(Defines.ProvidersSectionResource.Guid, "self", p => new { href = ProvidersHelper.GetSectionLocation(p.id) });
            hal.ProvideLink(Defines.ProvidersResource.Guid, "self", p => new { href = ProvidersHelper.GetProviderLocation(p.id) });

            // Rewrite -> Section
            hal.ProvideLink(Defines.Resource.Guid, Defines.ProvidersSectionResource.Name, rewrite => new { href = ProvidersHelper.GetSectionLocation(rewrite.id) });

            // Section -> providers
            hal.ProvideLink(Defines.ProvidersSectionResource.Guid, Defines.ProvidersResource.Name, providersSection => new { href = $"/{Defines.PROVIDERS_PATH}?{Defines.IDENTIFIER}={providersSection.id}" });
        }

        private void ConfigureGlobalRules()
        {
            var builder = Environment.Host.RouteBuilder;

            //
            // Use conditional hal to provide global rules only at webserver level
            var hal = Environment.Hal as IConditionalHalService;

            builder.MapWebApiRoute(Defines.GlobalRulesSectionResource.Guid, $"{Defines.GLOBAL_RULES_SECTION_PATH}/{{id?}}", new { controller = "GlobalRulesSection" });
            builder.MapWebApiRoute(Defines.GlobalRulesResource.Guid, $"{Defines.GLOBAL_RULES_PATH}/{{id?}}", new { controller = "GlobalRules" });

            if (hal != null) {
                hal.ProvideLink(Defines.GlobalRulesSectionResource.Guid, "self", ir => new { href = GlobalRulesHelper.GetSectionLocation(ir.id) });
                hal.ProvideLink(Defines.GlobalRulesResource.Guid, "self", ir => new { href = GlobalRulesHelper.GetRuleLocation(ir.id) });

                // Rewrite -> Section
                hal.ProvideLink(
                    Defines.Resource.Guid,
                    Defines.GlobalRulesSectionResource.Name,
                    rewrite => new { href = GlobalRulesHelper.GetSectionLocation(rewrite.id) },
                    rewrite => string.IsNullOrEmpty(rewrite.scope));

                // Section -> Rules
                hal.ProvideLink(Defines.GlobalRulesSectionResource.Guid, Defines.GlobalRulesResource.Name, globalRulesSection => new { href = $"/{Defines.GLOBAL_RULES_PATH}?{Defines.IDENTIFIER}={globalRulesSection.id}" });
            }
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
            hal.ProvideLink(Defines.InboundRulesSectionResource.Guid, Defines.InboundRulesResource.Name, inboundRulesSection => new { href = $"/{Defines.INBOUND_RULES_PATH}?{Defines.IDENTIFIER}={inboundRulesSection.id}" });
        }

        private void ConfigureOutboundRules()
        {
            var builder = Environment.Host.RouteBuilder;
            var hal = Environment.Hal;

            builder.MapWebApiRoute(Defines.OutboundRulesSectionResource.Guid, $"{Defines.OUTBOUND_RULES_SECTION_PATH}/{{id?}}", new { controller = "OutboundRulesSection" });
            builder.MapWebApiRoute(Defines.OutboundRulesResource.Guid, $"{Defines.OUTBOUND_RULES_PATH}/{{id?}}", new { controller = "OutboundRules" });
            builder.MapWebApiRoute(Defines.PreConditionsResource.Guid, $"{Defines.PRECONDITIONS_PATH}/{{id?}}", new { controller = "PreConditions" });
            builder.MapWebApiRoute(Defines.CustomTagsResource.Guid, $"{Defines.CUSTOM_TAGS_PATH}/{{id?}}", new { controller = "CustomTags" });

            // () -> Self
            hal.ProvideLink(Defines.OutboundRulesSectionResource.Guid, "self", ir => new { href = OutboundRulesHelper.GetSectionLocation(ir.id) });
            hal.ProvideLink(Defines.OutboundRulesResource.Guid, "self", ir => new { href = OutboundRulesHelper.GetRuleLocation(ir.id) });
            hal.ProvideLink(Defines.PreConditionsResource.Guid, "self", pc => new { href = OutboundRulesHelper.GetPreConditionLocation(pc.id) });
            hal.ProvideLink(Defines.CustomTagsResource.Guid, "self", tags => new { href = OutboundRulesHelper.GetCustomTagsLocation(tags.id) });

            // Rewrite -> Section
            hal.ProvideLink(Defines.Resource.Guid, Defines.OutboundRulesSectionResource.Name, rewrite => new { href = OutboundRulesHelper.GetSectionLocation(rewrite.id) });

            // Section -> Rules
            hal.ProvideLink(Defines.OutboundRulesSectionResource.Guid, Defines.OutboundRulesResource.Name, outboundRulesSection => new { href = $"/{Defines.OUTBOUND_RULES_PATH}?{Defines.IDENTIFIER}={outboundRulesSection.id}" });

            // Section -> PreConditions
            hal.ProvideLink(Defines.OutboundRulesSectionResource.Guid, Defines.PreConditionsResource.Name, outboundRulesSection => new { href = $"/{Defines.PRECONDITIONS_PATH}?{Defines.IDENTIFIER}={outboundRulesSection.id}" });

            // Section -> CustomTags
            hal.ProvideLink(Defines.OutboundRulesSectionResource.Guid, Defines.CustomTagsResource.Name, outboundRulesSection => new { href = $"/{Defines.CUSTOM_TAGS_PATH}?{Defines.IDENTIFIER}={outboundRulesSection.id}" });
        }

        private RewriteId GetRewriteId(Site s, string path)
        {
            return new RewriteId(s?.Id, path);
        }
    }
}

