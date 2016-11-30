// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Files
{
    using Core.Http;
    using AspNetCore.Mvc;
    using Sites;
    using Web.Administration;
    using System.Threading.Tasks;
    using Administration.Files;

    public class ContentController : ApiBaseController
    {
        private IFileProvider _fileService;

        public ContentController()
        {
            _fileService = FileProvider.Default;
        }

        [HttpHead]
        public IActionResult Head(string id)
        {
            FileId fileId = new FileId(id);
            Site site = SiteHelper.GetSite(fileId.SiteId);

            if (site == null) {
                return NotFound();
            }

            var physicalPath = FilesHelper.GetPhysicalPath(site, fileId.Path);

            if (!_fileService.FileExists(physicalPath)) {
                return NotFound();
            }

            AddHttpLinkHeader(fileId);

            return Context.GetFileContentHeaders(physicalPath, _fileService);
        }

        
        public async Task<IActionResult> Get(string id)
        {
            FileId fileId = new FileId(id);
            Site site = SiteHelper.GetSite(fileId.SiteId);

            if (site == null) {
                return NotFound();
            }

            var physicalPath = FilesHelper.GetPhysicalPath(site, fileId.Path);
            
            if (!_fileService.FileExists(physicalPath)) {
                return NotFound();
            }

            AddHttpLinkHeader(fileId);

            return await Context.GetFileContentAsync(physicalPath, _fileService);
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
            
            if (!_fileService.FileExists(physicalPath)) {
                return NotFound();
            }

            AddHttpLinkHeader(fileId);

            return await Context.PutFileContentAsync(physicalPath, _fileService);
        }



        private void AddHttpLinkHeader(FileId fileId)
        {
            Context.Response.Headers.Add("Link", $"</{Defines.FILES_PATH}/{fileId.Uuid}>; rel=\"meta\"; title=\"file metadata\", </{Defines.CONTENT_PATH}/{fileId.Uuid}>; rel=\"self\"; title=\"self\"");
        }
    }
}
