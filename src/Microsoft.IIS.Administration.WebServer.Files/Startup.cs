// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Files
{
    using AspNetCore.Builder;
    using Core;
    using Sites;
    using Web.Administration;
    using Core.Http;
    using Administration.Files;

    public class Startup : BaseModule
    {
        public Startup() { }

        public override void Start()
        {
            ConfigureFiles();
            ConfigureDirectories();
            ConfigureContent();
            ConfigureDownloads();
        }

        private void ConfigureFiles()
        {
            var router = Environment.Host.RouteBuilder;
            var hal = Environment.Hal;

            router.MapWebApiRoute(Defines.FilesResource.Guid, $"{Defines.FILES_PATH}/{{id?}}", new { controller = "files" });

            // Self
            hal.ProvideLink(Defines.FilesResource.Guid, "self", file => new { href = $"/{Defines.FILES_PATH}/{file.id}" });

            // Site
            Environment.Hal.ProvideLink(Sites.Defines.Resource.Guid, Defines.FilesResource.Name, site => {
                var siteId = new SiteId((string)site.id);
                Site s = SiteHelper.GetSite(siteId.Id);
                var id = new FileId(siteId.Id, "/");
                return new { href = $"{Defines.FILES_PATH}/{id.Uuid}" };
            });
        }

        private void ConfigureDirectories()
        {
            var router = Environment.Host.RouteBuilder;
            var hal = Environment.Hal;

            // Self
            hal.ProvideLink(Defines.DirectoriesResource.Guid, "self", file => new { href = $"/{Defines.FILES_PATH}/{file.id}" });

            // Directories
            hal.ProvideLink(Defines.DirectoriesResource.Guid, "files", file => new { href = $"/{Defines.FILES_PATH}?{Defines.PARENT_IDENTIFIER}={file.id}" });
        }

        private void ConfigureContent()
        {
            var router = Environment.Host.RouteBuilder;
            var hal = Environment.Hal;

            router.MapWebApiRoute(Defines.ContentResource.Guid, $"{Defines.CONTENT_PATH}/{{id?}}", new { controller = "content" });
            
            hal.ProvideLink(Defines.FilesResource.Guid, Defines.ContentResource.Name, file => new { href = $"/{Defines.CONTENT_PATH}/{file.id}" });
        }

        private void ConfigureDownloads()
        {
            var router = Environment.Host.RouteBuilder;
            var hal = Environment.Hal;

            router.MapWebApiRoute(Defines.DownloadResource.Guid, $"{Defines.DOWNLOAD_PATH}/{{id?}}", new { controller = "wsdownloads" });

            if (Environment.Host.ApplicationBuilder.ApplicationServices.GetService(typeof(IDownloadService)) != null) {
                hal.ProvideLink(Defines.FilesResource.Guid, Defines.DownloadResource.Name, file => new { href = $"{Defines.DOWNLOAD_PATH}" });
            }
        }
    }
}
