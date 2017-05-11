// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRequestTracing
{
    using Applications;
    using AspNetCore.Builder;
    using AspNetCore.Hosting;
    using Core;
    using Core.Http;
    using Extensions.DependencyInjection;
    using Extensions.DependencyInjection.Extensions;
    using Files;
    using Sites;
    using System.Linq;
    using Web.Administration;

    public class Startup : BaseModule, IServiceCollectionAccessor
    {
        public void Use(IServiceCollection services)
        {
            //
            // Chain freb xsl provider with previous file provider through aggregation

            ServiceDescriptor previous = services.FirstOrDefault(service => service.ServiceType == typeof(IFileProvider));

            if (previous != null) {
                ServiceDescriptor frebProvider = ServiceDescriptor.Singleton<IFileProvider>(sp => {
                    var env = (IHostingEnvironment)sp.GetService(typeof(IHostingEnvironment));
                    var configProvider = (IApplicationHostConfigProvider)sp.GetService(typeof(IApplicationHostConfigProvider));

                    return new FrebXslFileProvider((IFileProvider)(previous?.ImplementationInstance ?? previous?.ImplementationFactory(sp)), env, configProvider);
                });

                services.Replace(frebProvider);
            }
        }

        public override void Start()
        {
            ConfigureXsl();
            ConfigureHttpRequestTracing();
            ConfigureProviders();
            ConfigureRules();
            ConfigureTraces();
        }

        private void ConfigureXsl()
        {
            var services = Environment.Host.ApplicationBuilder.ApplicationServices;
            var downloads = (IDownloadService)services.GetService(typeof(IDownloadService));
            var redirects = (IFileRedirectService)services.GetService(typeof(IFileRedirectService));

            if (redirects != null && downloads != null) {
                IDownload download = downloads.Create(FrebXslFileInfo.FILE_NAME, null /* Never expires */);
                redirects.AddRedirect(download.PhysicalPath, () => download.Href, false);
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

        private void ConfigureTraces()
        {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.TracesResource.Guid, $"{ Defines.TRACES_PATH}/{{id?}}", new { controller = "RequestTraces" });

            Environment.Hal.ProvideLink(Defines.TracesResource.Guid, "self", r => new { href = TracesHelper.GetLocation(r.id) });

            Environment.Hal.ProvideLink(Defines.Resource.Guid, Defines.TracesResource.Name, hrt => new { href = $"/{Defines.TRACES_PATH}?{Defines.IDENTIFIER}={hrt.id}" });
        }
    }
}
