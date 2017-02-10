// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRequestTracing
{
    using Applications;
    using AspNetCore.Builder;
    using AspNetCore.Hosting;
    using Core;
    using Core.Http;
    using Files;
    using Sites;
    using Web.Administration;

    public class Startup : BaseModule
    {
        public override void Start()
        {
            ConfigureXsl();
            ConfigureHttpRequestTracing();
            ConfigureProviders();
            ConfigureRules();
        }

        private void ConfigureXsl()
        {
            var services = Environment.Host.ApplicationBuilder.ApplicationServices;
            var redirecter = (IFileRedirectService) services.GetService(typeof(IFileRedirectService));

            if (redirecter != null) {
                var hostingEnv = (IHostingEnvironment)services.GetService(typeof(IHostingEnvironment));
                var configProvider = (IApplicationHostConfigProvider)services.GetService(typeof(IApplicationHostConfigProvider));
                var xslLocator = new XslLocator(hostingEnv, configProvider);

                redirecter.AddRedirect("freb.xsl", () => xslLocator.GetPath(), true);
            }
        }

        private void ConfigureHttpRequestTracing()
        {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.Resource.Guid, $"{Defines.PATH}/{{id?}}", new { controller = "HttpRequestTracing" });

            // Self
            Environment.Hal.ProvideLink(Defines.Resource.Guid, "self", hrt => new { href = Helper.GetLocation(hrt.id) });

            // Web Server
            Environment.Hal.ProvideLink(WebServer.Defines.Resource.Guid, Defines.Resource.Name, _ => {
                var id = new HttpRequestTracingId(null, null, Helper.IsSectionLocal(null, null));
                return new { href = Helper.GetLocation(id.Uuid) };
            });

            // Site
            Environment.Hal.ProvideLink(Sites.Defines.Resource.Guid, Defines.Resource.Name, site => {
                var siteId = new SiteId((string)site.id);
                Site s = SiteHelper.GetSite(siteId.Id);
                var id = new HttpRequestTracingId(siteId.Id, "/", Helper.IsSectionLocal(s, "/"));
                return new { href = Helper.GetLocation(id.Uuid) };
            });

            // Application
            Environment.Hal.ProvideLink(Applications.Defines.Resource.Guid, Defines.Resource.Name, app => {
                var appId = new ApplicationId((string)app.id);
                Site s = SiteHelper.GetSite(appId.SiteId);
                var id = new HttpRequestTracingId(appId.SiteId, appId.Path, Helper.IsSectionLocal(s, appId.Path));
                return new { href = Helper.GetLocation(id.Uuid) };
            });
        }

        private void ConfigureProviders()
        {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.ProvidersResource.Guid, $"{ Defines.PROVIDERS_PATH}/{{id?}}", new { controller = "TraceProviders" });

            Environment.Hal.ProvideLink(Defines.ProvidersResource.Guid, "self", p => new { href = ProvidersHelper.GetLocation(p.id) });

            Environment.Hal.ProvideLink(Defines.Resource.Guid, Defines.ProvidersResource.Name, hrt => new { href = $"/{Defines.PROVIDERS_PATH}?{Defines.IDENTIFIER}={hrt.id}" });
        }

        private void ConfigureRules()
        {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.RulesResource.Guid, $"{ Defines.RULES_PATH}/{{id?}}", new { controller = "TraceRules" });

            Environment.Hal.ProvideLink(Defines.RulesResource.Guid, "self", r => new { href = RulesHelper.GetLocation(r.id) });

            Environment.Hal.ProvideLink(Defines.Resource.Guid, Defines.RulesResource.Name, hrt => new { href = $"/{Defines.RULES_PATH}?{Defines.IDENTIFIER}={hrt.id}" });
        }
    }
}
