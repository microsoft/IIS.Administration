// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Authorization
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
            ConfigureAuthorizationRules();
            ConfigureAuthorization();
        }

        private void ConfigureAuthorization()
        {
            var host = Environment.Host;
            var hal = Environment.Hal;

            host.RouteBuilder.MapWebApiRoute(Defines.AuthorizationResource.Guid, $"{Defines.AUTHORIZATION_PATH}/{{id?}}", new { controller = "authorization" });

            // Self
            hal.ProvideLink(Defines.AuthorizationResource.Guid, "self", authorization => new { href = $"/{Defines.AUTHORIZATION_PATH}/{authorization.id}" });

            // Web Server
            hal.ProvideLink(WebServer.Defines.Resource.Guid, Defines.AuthorizationResource.Name, _ => {
                var id = new AuthorizationId(null, null, AuthorizationHelper.IsSectionLocal(null, null));
                return new { href = $"/{Defines.AUTHORIZATION_PATH}/{id.Uuid}" };
            });

            // Site
            hal.ProvideLink(Sites.Defines.Resource.Guid, Defines.AuthorizationResource.Name, site => {
                var siteId = new SiteId((string)site.id);
                Site s = SiteHelper.GetSite(siteId.Id);
                var id = new AuthorizationId(siteId.Id, "/", AuthorizationHelper.IsSectionLocal(s, "/"));
                return new { href = $"/{Defines.AUTHORIZATION_PATH}/{id.Uuid}" };
            });

            // Application
            hal.ProvideLink(Applications.Defines.Resource.Guid, Defines.AuthorizationResource.Name, app => {
                var appId = new ApplicationId((string)app.id);
                Site s = SiteHelper.GetSite(appId.SiteId);
                var id = new AuthorizationId(appId.SiteId, appId.Path, AuthorizationHelper.IsSectionLocal(s, appId.Path));
                return new { href = $"/{Defines.AUTHORIZATION_PATH}/{id.Uuid}" };
            });
        }

        private void ConfigureAuthorizationRules()
        {
            var host = Environment.Host;
            var hal = Environment.Hal;

            // Top level resource routes for plugin
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.RulesResource.Guid, $"{ Defines.RULES_PATH}/{{id?}}", new { controller = "rules" });

            Environment.Hal.ProvideLink(Defines.RulesResource.Guid, "self", rule => new { href = $"/{Defines.RULES_PATH}/{rule.id}" });

            Environment.Hal.ProvideLink(Defines.AuthorizationResource.Guid, Defines.RulesResource.Name, auth => new { href = $"/{Defines.RULES_PATH}?{Defines.AUTHORIZATION_IDENTIFIER}={auth.id}" });
        }
    }
}
