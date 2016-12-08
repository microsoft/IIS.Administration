// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Files
{
    using AspNetCore.Builder;
    using Core;
    using Sites;
    using Web.Administration;
    using Core.Http;

    public class Startup : BaseModule
    {
        public Startup() { }

        public override void Start()
        {
            ConfigureFiles();
            ConfigureDirectories();
        }

        private void ConfigureFiles()
        {
            var router = Environment.Host.RouteBuilder;
            var hal = Environment.Hal;

            router.MapWebApiRoute(Defines.FilesResource.Guid, $"{Defines.FILES_PATH}/{{id?}}", new { controller = "wsfiles" });

            // Self
            hal.ProvideLink(Defines.FilesResource.Guid, "self", file => new { href = $"/{Defines.FILES_PATH}/{file.id}" });

            // Site
            Environment.Hal.ProvideLink(Sites.Defines.Resource.Guid, Defines.FilesResource.Name, site => {
                var siteId = new SiteId((string)site.id);
                Site s = SiteHelper.GetSite(siteId.Id);
                var id = new FileId(siteId.Id, "/");
                return new { href = $"/{Defines.FILES_PATH}/{id.Uuid}" };
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
    }
}
