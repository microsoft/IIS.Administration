// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Core.Utils;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Net;
    using System.Reflection;

    public class FileDownloadsController : ApiBaseController
    {
        private const int DEFAULT_DOWNLOAD_TIMEOUT = 5000; // milliseconds
        private IDownloadService _downloadService;
        private IFileProvider _fileService;

        public FileDownloadsController(IServiceProvider serviceProvider, IFileProvider fileProvider)
        {
            _fileService = fileProvider;
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
        [Audit]
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

            FileId fileId = FileId.FromUuid(fileUuid);
            IFileInfo file = _fileService.GetFile(fileId.PhysicalPath);

            if (!file.Exists) {
                throw new NotFoundException(fileId.PhysicalPath);
            }

            var dl = _downloadService.Create(file.Path, ttl ?? DEFAULT_DOWNLOAD_TIMEOUT);

            // Inform client location points to downloadable attachment
            Context.Response.Headers.Add("Pragma", "attachment");

            return Created(dl.Href, null);
        }
    }
}
