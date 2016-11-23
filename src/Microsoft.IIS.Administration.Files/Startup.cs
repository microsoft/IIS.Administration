// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using AspNetCore.Builder;
    using Core;
    using Core.Http;
    using Extensions.Caching.Memory;
    using Extensions.DependencyInjection;

    public class Startup : BaseModule, IServiceCollectionAccessor
    {
        public Startup() { }

        public void Use(IServiceCollection services)
        {
            services.AddSingleton<IDownloadService>(sp => {
                return new DownloadService((IMemoryCache)sp.GetService(typeof(IMemoryCache)));
            });
        }

        public override void Start() {
            ConfigureDownloads();
        }


        private void ConfigureDownloads()
        {
            var router = Environment.Host.RouteBuilder;
            var hal = Environment.Hal;

            router.MapWebApiRoute(Defines.DownloadResource.Guid, $"{Defines.DOWNLOAD_PATH}/{{id?}}", new { controller = "downloads" });
        }
    }
}
