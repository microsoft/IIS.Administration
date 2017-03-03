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
        }

        public override void Start()
        {
            ConfigureCertificates();
            ConfigureExports();
            ConfigureImports();
        }

        public void ConfigureCertificates()
        {
            var builder = Environment.Host.RouteBuilder;
            var hal = Environment.Hal;

            builder.MapWebApiRoute(Defines.Resource.Guid, $"{Defines.PATH}/{{id?}}", new { controller = "Certificates" });

            hal.ProvideLink(Defines.Resource.Guid, "self", cert => new { href = $"/{Defines.PATH}/{cert.id}" });

            hal.ProvideLink(Globals.ApiResource.Guid, Defines.Resource.Name, _ => new { href = $"/{Defines.PATH}" });
        }

        public void ConfigureExports()
        {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.ExportsResource.Guid, $"{Defines.EXPORTS_PATH}/{{id?}}", new { controller = "CertificateExports" });
        }

        public void ConfigureImports()
        {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.ImportsResource.Guid, $"{Defines.IMPORTS_PATH}/{{id?}}", new { controller = "CertificateImports" });
        }
    }
}
