// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.CentralCertificates
{
    using AspNetCore.Builder;
    using Certificates;
    using Core;
    using Core.Http;
    using Extensions.DependencyInjection;
    using System.Linq;

    public class Startup : BaseModule
    {
        private const string STORE_NAME = "IIS Central Certificate Store";
        internal static CentralCertificateStore CentralCertificateStore { get; private set; }

        public Startup() { }

        public override void Start()
        {
            ConfigureCentralCerts();
            ConfigureCertificates();
            ConfigureStoreProvider();
        }

        private void ConfigureCentralCerts()
        {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.Resource.Guid, $"{Defines.PATH}/{{id?}}", new { controller = "CentralCerts" });

            Environment.Hal.ProvideLink(Defines.Resource.Guid, "self", cc => new { href = $"{Defines.PATH}/{new CentralCertConfigId().Uuid}" });
            Environment.Hal.ProvideLink(WebServer.Defines.Resource.Guid, Defines.Resource.Name, _ => new { href = $"/{Defines.PATH}/{new CentralCertConfigId().Uuid}" });
        }

        private void ConfigureCertificates()
        {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.CertificatesResource.Guid, $"{Defines.CERTIFICATES_PATH}/{{id?}}", new { controller = "CentralCertsCertificates" });

            Environment.Hal.ProvideLink(Defines.Resource.Guid, Defines.CertificatesResource.Name, _ => {
                return new { href = $"/{Certificates.Defines.PATH}?{Certificates.Defines.StoreIdentifier}={StoreId.FromName(CentralCertificateStore.Name).Uuid}" };
            });
        }

        private void ConfigureStoreProvider()
        {
            ICertificateOptions options = Environment.Host.ApplicationBuilder.ApplicationServices.GetRequiredService<ICertificateOptions>();
            ICertificateStoreProvider storeProvider = Environment.Host.ApplicationBuilder.ApplicationServices.GetRequiredService<ICertificateStoreProvider>();

            var ccsOptions = options.Stores.FirstOrDefault(s => s.Name.Equals(STORE_NAME, System.StringComparison.OrdinalIgnoreCase));

            var store = new CentralCertificateStore(STORE_NAME, ccsOptions != null ? ccsOptions.Claims : new string[] { "read" });
            storeProvider.AddStore(store);
            CentralCertificateStore = store;
        }
    }
}
