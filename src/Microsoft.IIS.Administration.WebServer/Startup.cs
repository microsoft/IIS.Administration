// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer
{
    using AspNetCore.Builder;
    using Certificates;
    using Core;
    using Core.Http;
    using Extensions.DependencyInjection;
    using System.Collections.Generic;
    using Enum = System.Enum;

    public class Startup : BaseModule, IServiceCollectionAccessor
    {
        public Startup() { }

        public void Use(IServiceCollection services)
        {
            services.AddSingleton<IApplicationHostConfigProvider>(sp => new ApplicationHostConfigProvider(null));
            services.AddSingleton<IWebServerFeatureManager>(sp => new WebServerFeatureManager());
        }

        public override void Start()
        {
            ConfigureWebServer();
            ConfigureTransactions();
            ConfigureWebServerFeature();
            ConfigureCertificateOptions();
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

        private void ConfigureCertificateOptions()
        {
            ICertificateOptions options = (ICertificateOptions)Environment.Host.ApplicationBuilder.ApplicationServices.GetService(typeof(ICertificateOptions));

            options.AddStore(new CertStore() {
                Name = "My",
                Claims = new List<string> {
                        Enum.GetName(typeof(Access), Access.Read)
                    }
            });
            options.AddStore(new CertStore() {
                Name = "WebHosting",
                Claims = new List<string> {
                        Enum.GetName(typeof(Access), Access.Read),
                        Enum.GetName(typeof(Access), Access.Write),
                        Enum.GetName(typeof(Access), Access.Export),
                    }
            });
        }
    }
}
