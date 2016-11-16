// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Files
{
    using Core.Http;
    using AspNetCore.Mvc;
    using Sites;
    using System.IO;
    using Web.Administration;
    using System.Threading.Tasks;
    using Administration.Files;

    public class ContentController : ApiBaseController
    {
        [HttpGet]
        public async Task<IActionResult> Get(string id)
        {
            FileId fileId = new FileId(id);
            Site site = SiteHelper.GetSite(fileId.SiteId);

            if (site == null) {
                return NotFound();
            }

            var physicalPath = FilesHelper.GetPhysicalPath(site, fileId.Path);

            if (!File.Exists(physicalPath)) {
                return NotFound();
            }

            return await Context.GetFileContentAsync(new FileInfo(physicalPath));
        }

        [HttpPut]
        public async Task<IActionResult> Put(string id)
        {
            FileId fileId = new FileId(id);
            Site site = SiteHelper.GetSite(fileId.SiteId);

            if (site == null) {
                return NotFound();
            }

            var physicalPath = FilesHelper.GetPhysicalPath(site, fileId.Path);

            if (!File.Exists(physicalPath)) {
                return NotFound();
            }

            return await Context.PutFileContentAsync(new FileInfo(physicalPath));
        }
    }
}
