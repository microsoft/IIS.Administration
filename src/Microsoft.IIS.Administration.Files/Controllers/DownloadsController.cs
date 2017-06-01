// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using AspNetCore.Http;
    using AspNetCore.Mvc;
    using Core.Http;
    using Microsoft.AspNetCore.Authorization;
    using System.IO;
    using System.Threading.Tasks;


    [AllowAnonymous]
    public class DownloadsController : ApiBaseController
    {
        private IFileProvider _fileProvider;
        private IDownloadService _downloadService;
        private IFileRedirectService _redirectService;

        public DownloadsController(IDownloadService service,
                                   IFileProvider fileProvider,
                                   IFileRedirectService redirectService)
        {
            _fileProvider = fileProvider;
            _downloadService = service;
            _redirectService = redirectService;
        }

        [HttpHead]
        public IActionResult Head(string id)
        {
            var dl = _downloadService.Get(id);

            if (dl == null) {
                return NotFound();
            }

            if (!_fileProvider.GetFile(dl.PhysicalPath).Exists) {
                _downloadService.Remove(dl.Id);
                return NotFound();
            }

            Context.Response.WriteFileContentHeaders(dl.PhysicalPath, _fileProvider);

            return new EmptyResult();
        }

        [HttpGet]
        public async Task<IActionResult> Get(string id)
        {
            //
            // Check for redirect
            var redirect = _redirectService.GetRedirect(id);
            if (redirect != null) {
                return new RedirectResult(redirect.To(), redirect.Permanent);
            }

            //
            // Serve content
            var dl = _downloadService.Get(id);

            if (dl == null) {
                return NotFound();
            }

            if (!_fileProvider.GetFile(dl.PhysicalPath).Exists) {
                _downloadService.Remove(dl.Id);
                return NotFound();
            }

            bool inline = Context.Request.Query["inline"].Count > 0;

            IHeaderDictionary headers = new HeaderDictionary();
            headers.SetContentDisposition(inline, Path.GetFileName(dl.PhysicalPath));

            await Context.Response.WriteFileContentAsync(dl.PhysicalPath, _fileProvider, headers);

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
