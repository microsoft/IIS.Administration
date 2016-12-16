// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using AspNetCore.Mvc;
    using Core.Http;
    using System.Threading.Tasks;

    public class DownloadsController : ApiBaseController
    {
        private IFileProvider _fileProvider;
        private IDownloadService _downloadService;

        public DownloadsController(IDownloadService service)
        {
            _fileProvider = FileProvider.Default;
            _downloadService = service;
        }

        [HttpHead]
        public IActionResult Head(string id)
        {
            var dl = _downloadService.Get(id);

            if (dl == null) {
                return NotFound();
            }

            if (!_fileProvider.FileExists(dl.PhysicalPath)) {
                _downloadService.Remove(dl.Id);
                return NotFound();
            }

            Context.Response.WriteFileContentHeaders(dl.PhysicalPath, _fileProvider);

            return new EmptyResult();
        }

        [HttpGet]
        public async Task<IActionResult> Get(string id)
        {
            var dl = _downloadService.Get(id);

            if (dl == null) {
                return NotFound();
            }

            if (!_fileProvider.FileExists(dl.PhysicalPath)) {
                _downloadService.Remove(dl.Id);
                return NotFound();
            }

            await Context.Response.WriteFileContentAsync(dl.PhysicalPath, _fileProvider);

            return new EmptyResult();
        }

        [HttpDelete]
        public IActionResult Delete(string id)
        {
            _downloadService.Remove(id);

            return new NoContentResult();
        }
    }
}
