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
        private const string nameKey = "name";
        private const string physicalPathKey = "physical_path";

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
            string name = Context.Request.Query[nameKey];
            string physicalPath = Context.Request.Query[physicalPathKey];

            var files = new List<object>();
            Fields fields = Context.Request.GetFields();

            if (!string.IsNullOrEmpty(name)) {
                if (name.IndexOfAny(PathUtil.InvalidFileNameChars) != -1) {
                    throw new ApiArgumentException(nameKey);
                }
            }

            if (physicalPath != null) {
                return GetByPhysicalPath(physicalPath, fields);
            }

            if (parentUuid != null) {
                var fileId = FileId.FromUuid(parentUuid);

                if (!_provider.DirectoryExists(fileId.PhysicalPath)) {
                    return NotFound();
                }

                FillWithChildren(files, fileId.PhysicalPath, $"*{name}*", fields);
            }
            else {
                FillFromLocations(files, name, fields);
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
        [Audit]
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

            DateTime? created = DynamicHelper.To<DateTime>(model.created);
            DateTime? lastAccess = DynamicHelper.To<DateTime>(model.last_access);
            DateTime? lastModified = DynamicHelper.To<DateTime>(model.last_modified);

            var creationPath = Path.Combine(fileId.PhysicalPath, name);

            if (_provider.DirectoryExists(creationPath) || _provider.FileExists(creationPath)) {
                throw new AlreadyExistsException("name");
            }

            FileSystemInfo info = fileType == FileType.File ? (FileSystemInfo) _provider.CreateFile(creationPath) : 
                                                                               _provider.CreateDirectory(creationPath);
            
            info.CreationTime = created ?? info.CreationTime;
            info.LastAccessTime = lastAccess ?? info.LastAccessTime;
            info.LastWriteTime = lastModified ?? info.LastWriteTime;

            dynamic file = fileType == FileType.File ? FilesHelper.FileToJsonModel((FileInfo) info) :
                                               FilesHelper.DirectoryToJsonModel((DirectoryInfo) info);

            return Created(FilesHelper.GetLocation(file.id), file);
        }

        [HttpPatch]
        [Audit]
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
        [Audit]
        public IActionResult Delete(string id)
        {
            FileId fileId = FileId.FromUuid(id);

            if (_provider.FileExists(fileId.PhysicalPath) || _provider.DirectoryExists(fileId.PhysicalPath)) {
                switch (FilesHelper.GetFileType(fileId.PhysicalPath)) {
                    case FileType.File:
                        _provider.DeleteFile(fileId.PhysicalPath);
                        break;
                    case FileType.Directory:
                        _provider.DeleteDirectory(fileId.PhysicalPath);
                        break;
                    default:
                        break;
                }
            }
            return new NoContentResult();
        }



        private object GetByPhysicalPath(string physicalPath, Fields fields)
        {
            if (string.IsNullOrEmpty(physicalPath)) {
                throw new ApiArgumentException(physicalPathKey);
            }

            physicalPath = System.Environment.ExpandEnvironmentVariables(physicalPath);

            if (!PathUtil.IsPathRooted(physicalPath)) {
                throw new ApiArgumentException(physicalPathKey);
            }

            try {
                physicalPath = PathUtil.GetFullPath(physicalPath);
            }
            catch (ArgumentException) {
                throw new ApiArgumentException(physicalPathKey);
            }

            object model = null;

            if (_provider.FileExists(physicalPath)) {
                model = FilesHelper.FileToJsonModel(_provider.GetFileInfo(physicalPath), fields);
            }
            else if (_provider.DirectoryExists(physicalPath)) {
                model = FilesHelper.DirectoryToJsonModel(_provider.GetDirectoryInfo(physicalPath), fields);
            }

            if (model == null) {
                return NotFound();
            }

            return model;
        }

        private void FillWithChildren(List<object> models, string physicalPath, string nameFilter, Fields fields)
        {
            //
            // Directories
            foreach (var d in _provider.GetDirectories(physicalPath, nameFilter)) {
                if (!d.Attributes.HasFlag(FileAttributes.Hidden) && !d.Attributes.HasFlag(FileAttributes.System)) {
                    models.Add(FilesHelper.DirectoryToJsonModelRef(d, fields));
                }
            }

            //
            // Files
            foreach (var f in _provider.GetFiles(physicalPath, nameFilter)) {
                if (!f.Attributes.HasFlag(FileAttributes.Hidden) && !f.Attributes.HasFlag(FileAttributes.System)) {
                    models.Add(FilesHelper.FileToJsonModelRef(f, fields));
                }
            }
        }

        private void FillFromLocations(List<object> models, string nameFilter, Fields fields)
        {
            foreach (var location in _options.Locations) {
                if (_provider.IsAccessAllowed(location.Path, FileAccess.Read) && 
                        (string.IsNullOrEmpty(nameFilter) || new FileInfo(location.Path).Name.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) != -1)) {

                    models.Add(FilesHelper.DirectoryToJsonModelRef(_provider.GetDirectoryInfo(location.Path), fields));
                }
            }
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
