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
    using System.Collections.Generic;
    using System.IO;
    using System.Net;

    public class FilesController : ApiBaseController
    {
        IFileOptions _options;
        IFileProvider _provider = FileProvider.Default;

        public FilesController(IFileOptions options)
        {
            _options = options;
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.FilesName)]
        public object Get()
        {
            string parentUuid = Context.Request.Query[Defines.PARENT_IDENTIFIER];
            var files = new List<object>();

            Fields fields = Context.Request.GetFields();

            if (parentUuid != null) {
                var fileId = FileId.FromUuid(parentUuid);

                if (!_provider.DirectoryExists(fileId.PhysicalPath)) {
                    return NotFound();
                }

                //
                // Files
                foreach (var f in _provider.GetFiles(fileId.PhysicalPath, "*")) {
                    files.Add(FilesHelper.FileToJsonModelRef(f, fields));
                }

                //
                // Directories
                foreach (var d in _provider.GetDirectories(fileId.PhysicalPath, "*")) {
                    files.Add(FilesHelper.DirectoryToJsonModelRef(d, fields));
                }
            }
            else {
                foreach (var location in _options.Locations) {
                    if (_provider.IsAccessAllowed(location.Path, FileAccess.Read)) {
                        var model = FilesHelper.DirectoryToJsonModelRef(_provider.GetDirectoryInfo(location.Path), fields);
                        files.Add(FilesHelper.DirectoryToJsonModelRef(_provider.GetDirectoryInfo(location.Path), fields));
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
        [ResourceInfo(Name = Defines.FileName)]
        public object Get(string id)
        {
            FileId fileId = FileId.FromUuid(id);

            if (!_provider.FileExists(fileId.PhysicalPath) && !_provider.DirectoryExists(fileId.PhysicalPath)) {
                return NotFound();
            }

            return FilesHelper.ToJsonModel(fileId.PhysicalPath);
        }

        [HttpPost]
        [ResourceInfo(Name = Defines.FileName)]
        public object Post([FromBody] dynamic model)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (model.parent == null) {
                throw new ApiArgumentException("parent");
            }
            if (!(model.parent is JObject)) {
                throw new ApiArgumentException("parent", ApiArgumentException.EXPECTED_OBJECT);
            }

            //
            // Check Id
            string parentUuid = DynamicHelper.Value(model.parent.id);
            if (parentUuid == null) {
                throw new ApiArgumentException("parent.id");
            }

            FileId fileId = FileId.FromUuid(parentUuid);

            if (!_provider.DirectoryExists(fileId.PhysicalPath)) {
                throw new NotFoundException("parent");
            }

            //
            // Check Name
            string name = DynamicHelper.Value(model.name);

            if (!PathUtil.IsValidFileName(name)) {
                throw new ApiArgumentException("model.name");
            }

            //
            // Check Type
            string type = DynamicHelper.Value(model.type);

            FileType fileType;
            if (type == null || !Enum.TryParse(type, true, out fileType)) {
                throw new ApiArgumentException("model.type");
            }

            var creationPath = Path.Combine(fileId.PhysicalPath, name);

            if (_provider.DirectoryExists(creationPath) || _provider.FileExists(creationPath)) {
                throw new AlreadyExistsException("name");
            }

            dynamic file = null;

            //
            // Create
            if (fileType == FileType.File) {
                var fInfo = _provider.CreateFile(creationPath);

                file = FilesHelper.FileToJsonModel(fInfo);
            }
            else {
                var dInfo = _provider.CreateDirectory(creationPath);

                file = FilesHelper.DirectoryToJsonModel(dInfo);
            }

            return Created(FilesHelper.GetLocation(file.id), file);
        }

        [HttpPatch]
        [ResourceInfo(Name = Defines.FileName)]
        public object Patch([FromBody] dynamic model, string id)
        {
            FileId fileId = FileId.FromUuid(id);

            if (!_provider.FileExists(fileId.PhysicalPath) && !_provider.DirectoryExists(fileId.PhysicalPath)) {
                return NotFound();
            }

            dynamic result = null;
            switch (FilesHelper.GetFileType(fileId.PhysicalPath)) {

                case FileType.File:
                    result = PatchFile(model, fileId);
                    break;

                case FileType.Directory:
                    result = PatchDirectory(model, fileId);
                    break;

                default:
                    return new StatusCodeResult((int)HttpStatusCode.MethodNotAllowed);
            }

            if (result.id != id) {
                return LocationChanged(FilesHelper.GetLocation(result.id), result);
            }

            return result;
        }

        [HttpDelete]
        public IActionResult Delete(string id)
        {
            FileId fileId = FileId.FromUuid(id);
            string physicalPath = null;

            if (_provider.FileExists(fileId.PhysicalPath) || _provider.DirectoryExists(fileId.PhysicalPath)) {
                switch (FilesHelper.GetFileType(fileId.PhysicalPath)) {
                    case FileType.File:
                        _provider.DeleteFile(physicalPath);
                        break;
                    case FileType.Directory:
                        _provider.DeleteDirectory(physicalPath);
                        break;
                    default:
                        break;
                }
            }
            return new NoContentResult();
        }



        private object PatchFile(dynamic model, FileId fileId)
        {
            var physicalPath = FilesHelper.UpdateFile(model, fileId.PhysicalPath);

            return FilesHelper.FileToJsonModel(_provider.GetFileInfo(physicalPath));
        }

        private object PatchDirectory(dynamic model, FileId fileId)
        {
            var physicalPath = FilesHelper.UpdateDirectory(model, fileId.PhysicalPath);

            return FilesHelper.DirectoryToJsonModel(_provider.GetDirectoryInfo(physicalPath));
        }
    }
}
