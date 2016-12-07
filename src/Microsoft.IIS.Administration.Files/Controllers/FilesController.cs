// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using AspNetCore.Mvc;
    using Core.Http;
    using Core.Utils;
    using System.Collections.Generic;
    using System.IO;

    public class FilesController : ApiBaseController
    {
        IFileOptions _options;
        IFileProvider _service = FileProvider.Default;

        public FilesController(IFileOptions options)
        {
            _options = options;
        }

        [HttpGet]
        public object Get()
        {
            string parentUuid = Context.Request.Query[Defines.PARENT_IDENTIFIER];
            var files = new List<object>();

            Fields fields = Context.Request.GetFields();

            if (parentUuid != null) {
                var fileId = FileId.FromUuid(parentUuid);

                if (!_service.DirectoryExists(fileId.PhysicalPath)) {
                    return NotFound();
                }

                //
                // Files
                foreach (var f in _service.GetFiles(fileId.PhysicalPath, "*")) {
                    files.Add(FilesHelper.FileToJsonModelRef(f, fields));
                }

                //
                // Directories
                foreach (var d in _service.GetDirectories(fileId.PhysicalPath, "*")) {
                    files.Add(FilesHelper.DirectoryToJsonModelRef(d, fields));
                }
            }
            else {
                foreach (var location in _options.Locations) {
                    if (_service.IsAccessAllowed(location.Path, FileAccess.Read)) {
                        files.Add(FilesHelper.DirectoryToJsonModelRef(_service.GetDirectoryInfo(location.Path), fields));
                    }
                }
            }

            // Set HTTP header for total count
            this.Context.Response.SetItemsCount(files.Count);

            return new
            {
                files = files
            };
        }

        [HttpGet]
        public object Get(string id)
        {
            FileId fileId = FileId.FromUuid(id);

            if (!_service.FileExists(fileId.PhysicalPath) && !_service.DirectoryExists(fileId.PhysicalPath)) {
                return NotFound();
            }

            return FilesHelper.ToJsonModel(fileId.PhysicalPath);
        }
    }
}
