// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using AspNetCore.Builder;
    using Core;
    using Core.Http;
    using Extensions.Caching.Memory;
    using Extensions.Configuration;
    using Extensions.DependencyInjection;

    public class Startup : BaseModule, IServiceCollectionAccessor
    {
        public Startup() { }

        public void Use(IServiceCollection services)
        {
            services.AddSingleton<IDownloadService>(sp => {
                return new DownloadService((IMemoryCache)sp.GetService(typeof(IMemoryCache)));
            });

            services.AddSingleton(sp => {
                return FileOptions.FromConfiguration((IConfiguration)sp.GetService(typeof(IConfiguration)));
            });
        }

        public override void Start()
        {
            ConfigureDownloads();
            ConfigureFiles();
            ConfigureApiDownloads();
            ConfigureContent();
            ConfigureCopy();
        }



        private void ConfigureDownloads()
        {
            var router = Environment.Host.RouteBuilder;

            router.MapWebApiRoute(Defines.DownloadResource.Guid, $"{Defines.DOWNLOAD_PATH}/{{id?}}", new { controller = "downloads" });
        }

        private void ConfigureFiles()
        {
            var router = Environment.Host.RouteBuilder;
            var hal = Environment.Hal;
            
            router.MapWebApiRoute(Defines.FilesResource.Guid, $"{Defines.FILES_PATH}/{{id?}}", new { controller = "files" });

            hal.ProvideLink(Globals.ApiResource.Guid, Defines.FilesResource.Name, _ => new { href = $"/{Defines.FILES_PATH}" });

            // Self (Files)
            hal.ProvideLink(Defines.FilesResource.Guid, "self", file => new { href = $"/{Defines.FILES_PATH}/{file.id}" });

            // Self (Directories)
            hal.ProvideLink(Defines.DirectoriesResource.Guid, "self", file => new { href = $"/{Defines.FILES_PATH}/{file.id}" });

            // Directories
            hal.ProvideLink(Defines.DirectoriesResource.Guid, "files", file => new { href = $"/{Defines.FILES_PATH}?{Defines.PARENT_IDENTIFIER}={file.id}" });
        }

        private void ConfigureContent()
        {
            var router = Environment.Host.RouteBuilder;
            var hal = Environment.Hal;

            router.MapWebApiRoute(Defines.ContentResource.Guid, $"{Defines.CONTENT_PATH}/{{id?}}", new { controller = "content" });

            // Files
            hal.ProvideLink(Defines.FilesResource.Guid, Defines.ContentResource.Name, file => new { href = $"/{Defines.CONTENT_PATH}/{file.id}" });
        }

        private void ConfigureApiDownloads()
        {
            var router = Environment.Host.RouteBuilder;
            var hal = Environment.Hal;

            router.MapWebApiRoute(Defines.ApiDownloadResource.Guid, $"{Defines.API_DOWNLOAD_PATH}/{{id?}}", new { controller = "FileDownloads" });

            hal.ProvideLink(Defines.FilesResource.Guid, Defines.ApiDownloadResource.Name, file => new { href = $"/{Defines.API_DOWNLOAD_PATH}" });
        }

        private void ConfigureCopy()
        {
            var router = Environment.Host.RouteBuilder;
            var hal = Environment.Hal;

            router.MapWebApiRoute(Defines.CopyResource.Guid, $"{Defines.COPY_PATH}", new { controller = "copy" });

            hal.ProvideLink(Defines.FilesResource.Guid, Defines.CopyResource.Name, file => new { href = $"/{Defines.COPY_PATH}" });
        }
    }
}
