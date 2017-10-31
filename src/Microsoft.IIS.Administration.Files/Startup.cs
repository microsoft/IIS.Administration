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
            services.AddSingleton<IDownloadService>(sp => new DownloadService((IMemoryCache)sp.GetService(typeof(IMemoryCache))));

            services.AddSingleton<IFileRedirectService>(sp => new FileRedirectService());
        }

        public override void Start()
        {
            ConfigureDownloads();
            ConfigureFiles();
            ConfigureApiDownloads();
            ConfigureContent();
            ConfigureCopy();
            ConfigureMove();
            ConfigureLocations();
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

            router.MapWebApiRoute(Defines.CopyResource.Guid, $"{Defines.COPY_PATH}/{{id?}}", new { controller = "copy" });

            // Self
            hal.ProvideLink(Defines.CopyResource.Guid, "self", copy => new { href = MoveHelper.GetLocation(copy.id, true) });

            hal.ProvideLink(Defines.FilesResource.Guid, Defines.CopyResource.Name, file => new { href = $"/{Defines.COPY_PATH}" });

            hal.ProvideLink(Defines.DirectoriesResource.Guid, Defines.CopyResource.Name, dir => new { href = $"/{Defines.COPY_PATH}" });
        }

        private void ConfigureMove()
        {
            var router = Environment.Host.RouteBuilder;
            var hal = Environment.Hal;

            router.MapWebApiRoute(Defines.MoveResource.Guid, $"{Defines.MOVE_PATH}/{{id?}}", new { controller = "move" });

            // Self
            hal.ProvideLink(Defines.MoveResource.Guid, "self", move => new { href = MoveHelper.GetLocation(move.id, false) });

            hal.ProvideLink(Defines.FilesResource.Guid, Defines.MoveResource.Name, file => new { href = $"/{Defines.MOVE_PATH}" });

            hal.ProvideLink(Defines.DirectoriesResource.Guid, Defines.MoveResource.Name, dir => new { href = $"/{Defines.MOVE_PATH}" });
        }

        private void ConfigureLocations()
        {
            var router = Environment.Host.RouteBuilder;
            var hal = Environment.Hal;

            router.MapWebApiRoute(Defines.LocationsResource.Guid, $"{Defines.LOCATIONS_PATH}/{{id?}}", new { controller = "locations" });

            // Self (Files)
            hal.ProvideLink(Defines.LocationsResource.Guid, "self", location => new { href = $"/{Defines.LOCATIONS_PATH}/{location.id}" });

            // File
            hal.ProvideLink(Defines.LocationsResource.Guid, "directory", location => new { href = $"/{Defines.FILES_PATH}/{location.id}" });
        }
    }
}
