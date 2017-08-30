// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Monitoring
{
    using AspNetCore.Builder;
    using Core;
    using Core.Http;
    using Microsoft.Extensions.DependencyInjection;

    public class Startup : BaseModule, IServiceCollectionAccessor
    {
        private WebServerMonitor _monitor = null;

        public Startup() { }

        public void Use(IServiceCollection services)
        {
            _monitor = new WebServerMonitor();
            services.AddSingleton<IWebServerMonitor>(_monitor);
        }

        public override void Start()
        {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.Resource.Guid, $"{Defines.PATH}", new { controller = "WebServerMonitoring" });

            Environment.Hal.ProvideLink(Defines.Resource.Guid, "self", _ => new { href = $"/{Defines.PATH}" });
            Environment.Hal.ProvideLink(WebServer.Defines.Resource.Guid, Defines.Resource.Name, _ => new { href = $"/{Defines.PATH}" });
        }

        public override void Stop()
        {
            if (_monitor != null) {
                _monitor.Dispose();
                _monitor = null;
            }
        }
    }
}
