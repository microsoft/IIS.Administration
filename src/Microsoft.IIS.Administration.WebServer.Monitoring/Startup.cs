// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Monitoring
{
    using AspNetCore.Builder;
    using Core;
    using Core.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.IIS.Administration.Monitoring;

    public class Startup : BaseModule, IServiceCollectionAccessor
    {
        private CounterProvider _provider = null;

        public Startup() { }

        public void Use(IServiceCollection services)
        {
            var finder = new CounterFinder((ICounterTranslator) new CounterTranslator());

            _provider = new CounterProvider(finder);

            var appPoolMonitor = new AppPoolMonitor(_provider);
            var webserverMonitor = new WebServerMonitor(_provider);
            var siteMonitor = new WebSiteMonitor(_provider);

            services.AddSingleton<ICounterProvider>(_provider);
            services.AddSingleton<IAppPoolMonitor>(appPoolMonitor);
            services.AddSingleton<IWebServerMonitor>(webserverMonitor);
            services.AddSingleton<IWebSiteMonitor>(siteMonitor);
        }

        public override void Start()
        {
            ConfigureWebServerMonitoring();
            ConfigureWebSiteMonitoring();
            ConfigureAppPoolMonitoring();
        }

        private void ConfigureWebServerMonitoring()
        {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.WebServerMonitoringResource.Guid, $"{Defines.PATH}/{{id?}}", new { controller = "WebServerMonitoring" });

            Environment.Hal.ProvideLink(Defines.WebServerMonitoringResource.Guid, "self", self => new { href = $"/{Defines.PATH}/{self.id}" });
            Environment.Hal.ProvideLink(WebServer.Defines.Resource.Guid, Defines.WebServerMonitoringResource.Name, webserver => new { href = $"/{Defines.PATH}/{webserver.id}" });
        }

        private void ConfigureWebSiteMonitoring()
        {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.WebSiteMonitoringResource.Guid, $"{Defines.WEBSITE_MONITORING_PATH}/{{id?}}", new { controller = "WebSiteMonitoring" });

            Environment.Hal.ProvideLink(Defines.WebSiteMonitoringResource.Guid, "self", self => new { href = $"/{Defines.WEBSITE_MONITORING_PATH}/{self.id}" });
            Environment.Hal.ProvideLink(Sites.Defines.Resource.Guid, Defines.WebSiteMonitoringResource.Name, site => new { href = $"/{Defines.WEBSITE_MONITORING_PATH}/{site.id}" });
        }

        private void ConfigureAppPoolMonitoring()
        {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.AppPoolMonitoringResource.Guid, $"{Defines.APP_POOL_MONITORING_PATH}/{{id?}}", new { controller = "AppPoolMonitoring" });

            Environment.Hal.ProvideLink(Defines.AppPoolMonitoringResource.Guid, "self", self => new { href = $"/{Defines.APP_POOL_MONITORING_PATH}/{self.id}" });
            Environment.Hal.ProvideLink(AppPools.Defines.Resource.Guid, Defines.AppPoolMonitoringResource.Name, pool => new { href = $"/{Defines.APP_POOL_MONITORING_PATH}/{pool.id}" });
        }

        public override void Stop()
        {
            if (_provider != null) {
                _provider.Dispose();
                _provider = null;
            }
        }
    }
}
