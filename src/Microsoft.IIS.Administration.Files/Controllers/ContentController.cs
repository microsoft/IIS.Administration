// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using Core.Http;
    using AspNetCore.Mvc;
    using System.Threading.Tasks;

    public class ContentController : ApiBaseController
    {
        private IFileProvider _fileProvider;

        public ContentController()
        {
            _fileProvider = FileProvider.Default;
        }

        [HttpHead]
        public IActionResult Head(string id)
        {
            FileId fileId = FileId.FromUuid(id);

            if (!_fileProvider.FileExists(fileId.PhysicalPath)) {
                return NotFound();
            }

            AddHttpLinkHeader(fileId);

            Context.Response.WriteFileContentHeaders(fileId.PhysicalPath, _fileProvider);

            return new EmptyResult();
        }

        [HttpGet]
        public async Task<IActionResult> Get(string id)
        {
            FileId fileId = FileId.FromUuid(id);

            if (!_fileProvider.FileExists(fileId.PhysicalPath)) {
                return NotFound();
            }

            AddHttpLinkHeader(fileId);

            await Context.Response.WriteFileContentAsync(fileId.PhysicalPath, _fileProvider);

            return new EmptyResult();
        }

        [HttpPut]
        public async Task<IActionResult> Put(string id)
        {
            FileId fileId = FileId.FromUuid(id);

            if (!_fileProvider.FileExists(fileId.PhysicalPath)) {
                return NotFound();
            }

            AddHttpLinkHeader(fileId);

            await Context.Response.PutFileContentAsync(fileId.PhysicalPath, _fileProvider);

            return new EmptyResult();
        }



        private void AddHttpLinkHeader(FileId fileId)
        {
            Context.Response.Headers.Add("Link", $"</{Defines.FILES_PATH}/{fileId.Uuid}>; rel=\"meta\"; title=\"file metadata\", </{Defines.CONTENT_PATH}/{fileId.Uuid}>; rel=\"self\"; title=\"self\"");
        }
    }
}
