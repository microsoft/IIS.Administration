// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer
{
    using AspNetCore.Builder;
    using Certificates;
    using Core;
    using Core.Http;
    using Extensions.DependencyInjection;
    using System.Linq;

    public class Startup : BaseModule, IServiceCollectionAccessor
    {
        public Startup() { }

        public void Use(IServiceCollection services)
        {
            services.AddSingleton<IApplicationHostConfigProvider>(new ApplicationHostConfigProvider(null));
            services.AddSingleton<IWebServerFeatureManager>(new WebServerFeatureManager());
            services.AddSingleton<IWebServerVersion>(new WebServerVersion());
        }

        public override void Start()
        {
            ConfigureWebServer();
            ConfigureTransactions();
            ConfigureWebServerFeature();
            ConfigureCertificateStoreProvider();
        }

        private void ConfigureWebServer()
        {
            var app = Environment.Host.ApplicationBuilder;
            var router = Environment.Host.RouteBuilder;
            var hal = Environment.Hal;

            app.UseMiddleware<Injector>(app.ApplicationServices.GetRequiredService<IApplicationHostConfigProvider>());

            router.MapWebApiRoute(Defines.Resource.Guid, $"{Defines.PATH}/{{id?}}", new { controller = "webserver" });

            hal.ProvideLink(Defines.Resource.Guid, "self", _ => new { href = $"/{Defines.PATH}" });
            hal.ProvideLink(Globals.ApiResource.Guid, Defines.Resource.Name, _ => new { href = $"/{Defines.PATH}" });
        }

        private void ConfigureTransactions()
        {
            var router = Environment.Host.RouteBuilder;
            var hal = Environment.Hal;

            router.MapWebApiRoute(Defines.TransactionsResource.Guid, $"{Defines.TRANSACTIONS_PATH}/{{id?}}", new { controller = "transactions" }, skipEdge: true);
            hal.ProvideLink(Defines.TransactionsResource.Guid, "self", trans => new { href = TransactionHelper.GetLocation(trans.id) });

            hal.ProvideLink(Defines.Resource.Guid, Defines.TransactionsResource.Name, _ => new { href = $"/{Defines.TRANSACTIONS_PATH}" });
        }

        private void ConfigureWebServerFeature()
        {
            WebServerFeatureManagerAccessor.Services = Environment.Host.ApplicationBuilder.ApplicationServices;
        }

        public void ConfigureCertificateStoreProvider()
        {
            ICertificateOptions options = Environment.Host.ApplicationBuilder.ApplicationServices.GetRequiredService<ICertificateOptions>();
            ICertificateStoreProvider storeProvider = Environment.Host.ApplicationBuilder.ApplicationServices.GetRequiredService<ICertificateStoreProvider>();
            IWebServerVersion versionProvider = Environment.Host.ApplicationBuilder.ApplicationServices.GetService<IWebServerVersion>();

            const string webHosting = "WebHosting";
            const string my = "My";

            //
            // My
            if (!storeProvider.Stores.Any(s => s.Name.Equals(my, System.StringComparison.OrdinalIgnoreCase)) && WindowsCertificateStore.Exists(my)) {
                storeProvider.AddStore(new WindowsCertificateStore(my, new string[] { "read" }));
            }

            if (versionProvider?.Version != null && versionProvider.Version >= new System.Version(8, 0)) {
                //
                // WebHosting Certificate Store was not introduced until IIS 8.0
                if (!storeProvider.Stores.Any(s => s.Name.Equals(webHosting, System.StringComparison.OrdinalIgnoreCase)) && WindowsCertificateStore.Exists(webHosting)) {
                    storeProvider.AddStore(new WindowsCertificateStore(webHosting, new string[] { "read" }));
                }
            }

        }
    }
}
