// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer
{
    using AspNetCore.Builder;
    using Core;
    using Core.Http;
    using Extensions.DependencyInjection;

    public class Startup : BaseModule, IServiceCollectionAccessor
    {
        public Startup() { }

        public void Use(IServiceCollection services)
        {
            services.AddSingleton<IApplicationHostConfigProvider>(sp => new ApplicationHostConfigProvider(null));
        }

        public override void Start()
        {
            ConfigureWebServer();
            ConfigureTransactions();
        }

        public void ConfigureWebServer()
        {
            var app = Environment.Host.ApplicationBuilder;
            var router = Environment.Host.RouteBuilder;
            var hal = Environment.Hal;

            app.UseMiddleware<Injector>(app.ApplicationServices.GetRequiredService<IApplicationHostConfigProvider>());

            router.MapWebApiRoute(Defines.Resource.Guid, $"{Defines.PATH}/{{id?}}", new { controller = "webserver" });

            hal.ProvideLink(Defines.Resource.Guid, "self", _ => new { href = $"/{Defines.PATH}" });
            hal.ProvideLink(Globals.ApiResource.Guid, Defines.Resource.Name, _ => new { href = $"/{Defines.PATH}" });
        }

        public void ConfigureTransactions()
        {
            var router = Environment.Host.RouteBuilder;
            var hal = Environment.Hal;

            router.MapWebApiRoute(Defines.TransactionsResource.Guid, $"{Defines.TRANSACTIONS_PATH}/{{id?}}", new { controller = "transactions" }, skipEdge: true);
            hal.ProvideLink(Defines.TransactionsResource.Guid, "self", trans => new { href = TransactionHelper.GetLocation(trans.id) });

            hal.ProvideLink(Defines.Resource.Guid, Defines.TransactionsResource.Name, _ => new { href = $"/{Defines.TRANSACTIONS_PATH}" });
        }
    }
}
