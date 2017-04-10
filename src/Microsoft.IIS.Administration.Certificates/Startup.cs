// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Certificates
{
    using AspNetCore.Builder;
    using Core;
    using Core.Http;
    using Extensions.Configuration;
    using Extensions.DependencyInjection;

    public class Startup : BaseModule, IServiceCollectionAccessor
    {
        public Startup() { }

        public void Use(IServiceCollection services)
        {
            services.AddSingleton<ICertificateOptions>(sp => CertificateOptions.FromConfiguration(sp.GetRequiredService<IConfiguration>()));
            services.AddSingleton<ICertificateStoreProvider>(sp => new CertificateStoreProvider());
        }

        public override void Start()
        {
            ConfigureCertificates();
            ConfigureStores();
            ConfigureStoreProvider();
        }

        public void ConfigureCertificates()
        {
            var builder = Environment.Host.RouteBuilder;
            var hal = Environment.Hal;

            builder.MapWebApiRoute(Defines.Resource.Guid, $"{Defines.PATH}/{{id?}}", new { controller = "Certificates" });

            //
            // Self
            hal.ProvideLink(Defines.Resource.Guid, "self", cert => new { href = $"/{Defines.PATH}/{cert.id}" });

            //
            // Webserver
            hal.ProvideLink(Globals.ApiResource.Guid, Defines.Resource.Name, _ => new { href = $"/{Defines.PATH}" });

            //
            // Certificate Store
            hal.ProvideLink(Defines.StoresResource.Guid, Defines.Resource.Name, store => new { href = $"/{Defines.PATH}?{Defines.StoreIdentifier}={store.id}" });
        }

        public void ConfigureStores()
        {
            var builder = Environment.Host.RouteBuilder;
            var hal = Environment.Hal;

            builder.MapWebApiRoute(Defines.StoresResource.Guid, $"{Defines.STORES_PATH}/{{id?}}", new { controller = "CertificateStores" });

            hal.ProvideLink(Defines.StoresResource.Guid, "self", store => new { href = $"/{Defines.STORES_PATH}/{store.id}" });
        }

        public void ConfigureStoreProvider()
        {
            CertificateStoreProviderAccessor.Services = Environment.Host.ApplicationBuilder.ApplicationServices;
            ICertificateOptions options = Environment.Host.ApplicationBuilder.ApplicationServices.GetRequiredService<ICertificateOptions>();
            ICertificateStoreProvider storeProvider = Environment.Host.ApplicationBuilder.ApplicationServices.GetRequiredService<ICertificateStoreProvider>();


            foreach (var store in options.Stores) {
                if (WindowsCertificateStore.Exists(store.Name)) {
                    storeProvider.AddStore(new WindowsCertificateStore(store.Name, store.Claims));
                }
            }
        }
    }
}
