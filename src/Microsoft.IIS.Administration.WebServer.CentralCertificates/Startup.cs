// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.CentralCertificates
{
    using AspNetCore.Builder;
    using Certificates;
    using Core;
    using Core.Http;
    using Extensions.DependencyInjection;
    using Files;
    using System.Linq;

    public class Startup : BaseModule
    {
        private const string STORE_NAME = "IIS Central Certificate Store";
        internal static CentralCertificateStore CentralCertificateStore { get; private set; }

        public Startup() { }

        public override void Start()
        {
            IWebServerVersion versionProvider = Environment.Host.ApplicationBuilder.ApplicationServices.GetService<IWebServerVersion>();
            
            if (versionProvider?.Version == null || versionProvider.Version < new System.Version(8,0)) {
                //
                // IIS Centralized Certificate Store was not introduced until IIS 8.0
                // Prevent provision of CCS functionality
                return;
            }

            ConfigureCentralCerts();
            ConfigureStoreProvider();
        }

        private void ConfigureCentralCerts()
        {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.Resource.Guid, $"{Defines.PATH}/{{id?}}", new { controller = "CentralCerts" });

            Environment.Hal.ProvideLink(Defines.Resource.Guid, "self", cc => new { href = $"/{Defines.PATH}/{ new CentralCertConfigId().Uuid}" });
            Environment.Hal.ProvideLink(WebServer.Defines.Resource.Guid, Defines.Resource.Name, _ => new { href = CentralCertHelper.GetLocation() });
        }

        private void ConfigureStoreProvider()
        {
            ICertificateOptions options = Environment.Host.ApplicationBuilder.ApplicationServices.GetRequiredService<ICertificateOptions>();
            ICertificateStoreProvider storeProvider = Environment.Host.ApplicationBuilder.ApplicationServices.GetRequiredService<ICertificateStoreProvider>();
            IFileProvider fileProvider = Environment.Host.ApplicationBuilder.ApplicationServices.GetRequiredService<IFileProvider>();

            var ccsOptions = options.Stores.FirstOrDefault(s => s.Name.Equals(STORE_NAME, System.StringComparison.OrdinalIgnoreCase));

            var store = new CentralCertificateStore(STORE_NAME, ccsOptions != null ? ccsOptions.Claims : new string[] { "read" }, fileProvider);

            if (CentralCertHelper.FeatureEnabled && store.Enabled) {
                storeProvider.AddStore(store);
            }

            CentralCertificateStore = store;
        }
    }
}
