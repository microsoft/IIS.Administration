// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Files
{
    using Administration.Files;
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Core.Utils;
    using Newtonsoft.Json.Linq;
    using Sites;
    using System;
    using System.Net;
    using System.Reflection;
    using Web.Administration;

    public class WsDownloadsController : ApiBaseController
    {
        private IDownloadService _downloadService;
        private IFileProvider _fileService;

        public WsDownloadsController(IServiceProvider serviceProvider)
        {
            _fileService = FileProvider.Default;
            _downloadService = (IDownloadService)serviceProvider.GetService(typeof(IDownloadService));
        }

        [HttpGet]
        [HttpHead]
        [HttpPatch]
        [HttpPut]
        public IActionResult NotAllowed()
        {
            return new StatusCodeResult((int)HttpStatusCode.MethodNotAllowed);
        }

        [HttpPost]
        public IActionResult Post([FromBody] dynamic model)
        {
            if (_downloadService == null) {
                throw new NotFoundException(typeof(IDownloadService).GetTypeInfo().Assembly.FullName);
            }

            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (model.file == null) {
                throw new ApiArgumentException("file");
            }
            if (!(model.file is JObject)) {
                throw new ApiArgumentException("file", ApiArgumentException.EXPECTED_OBJECT);
            }

            //
            // Check Id
            string fileUuid = DynamicHelper.Value(model.file.id);
            if (fileUuid == null) {
                throw new ApiArgumentException("file.id");
            }

            int? ttl = DynamicHelper.To<int>(model.ttl);

            FileId fileId = new FileId(fileUuid);
            Site site = SiteHelper.GetSite(fileId.SiteId);

            if (site == null) {
                return NotFound();
            }

            var physicalPath = FilesHelper.GetPhysicalPath(site, fileId.Path);

            if (!_fileService.FileExists(physicalPath)) {
                throw new NotFoundException(physicalPath);
            }

            var dl = _downloadService.Create(physicalPath, ttl);

            return Created(dl.Href, null);
        }
    }
}
