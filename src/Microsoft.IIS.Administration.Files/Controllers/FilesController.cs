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
    using System.Linq;
    using System.Net;

    public class FilesController : ApiBaseController
    {
        private const string _units = "files";
        private const string _nameKey = "name";
        private const string _physicalPathKey = "physical_path";

        IFileOptions _options;
        IFileProvider _provider;
        FilesHelper _helper;

        public FilesController(IFileOptions options, IFileProvider fileProvider)
        {
            _options = options;
            _provider = fileProvider;
            _helper = new FilesHelper(fileProvider, options);
        }

        [HttpHead]
        [ResourceInfo(Name = Defines.FilesName)]
        public object Head()
        {
            Fields fields;
            FileId parentId;
            string nameFilter, physicalPath;

            PreGet(out parentId, out nameFilter, out physicalPath, out fields);

            if (physicalPath != null) {
                return GetByPhysicalPath(physicalPath, fields);
            }

            if (parentId != null && !_provider.DirectoryExists(parentId.PhysicalPath)) {
                return NotFound();
            }

            IEnumerable<IFileInfo> children = parentId != null ? GetChildren(parentId.PhysicalPath, nameFilter) : GetFromLocations(nameFilter);

            // Set HTTP header for total count
            this.Context.Response.SetItemsCount(children.Count());
            this.Context.Response.Headers[HeaderNames.AcceptRanges] = _units;

            return Ok();
        }


        [HttpGet]
        [ResourceInfo(Name = Defines.FilesName)]
        public object Get()
        {
            Fields fields;
            FileId parentId;
            string nameFilter, physicalPath;

            PreGet(out parentId, out nameFilter, out physicalPath, out fields);

            if (physicalPath != null) {
                return GetByPhysicalPath(physicalPath, fields);
            }
            
            if (parentId != null && !_provider.DirectoryExists(parentId.PhysicalPath)) {
                return NotFound();
            }

            var models = new List<object>();
            IEnumerable<IFileInfo> children = parentId != null ? GetChildren(parentId.PhysicalPath, nameFilter) : GetFromLocations(nameFilter);

            foreach (IFileInfo child in children) {
                models.Add(_helper.ToJsonModelRef(child, fields));
            }

            // Set HTTP header for total count
            this.Context.Response.SetItemsCount(models.Count);
            this.Context.Response.Headers[HeaderNames.AcceptRanges] = _units;

            return new
            {
                files = models
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

            return _helper.ToJsonModel(fileId.PhysicalPath);
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

            IFileInfo info = fileType == FileType.File ? _provider.CreateFile(creationPath) : _provider.CreateDirectory(creationPath);

            _provider.SetFileTime(info.Path, lastAccess, lastModified, created);

            dynamic file = _helper.ToJsonModel(info);

            return Created(FilesHelper.GetLocation(file.id), _helper.ToJsonModel(info));
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
                    case FileType.Directory:
                        _provider.Delete(fileId.PhysicalPath);
                        break;
                    default:
                        break;
                }
            }
            return new NoContentResult();
        }


        
        private void PreGet(out FileId parentId, out string name, out string physicalPath, out Fields fields)
        {
            string parentUuid = Context.Request.Query[Defines.PARENT_IDENTIFIER];
            name = Context.Request.Query[_nameKey];
            physicalPath = Context.Request.Query[_physicalPathKey];
            fields = Context.Request.GetFields();

            if (!string.IsNullOrEmpty(name)) {
                if (name.IndexOfAny(PathUtil.InvalidFileNameChars) != -1) {
                    throw new ApiArgumentException(_nameKey);
                }
            }

            parentId = string.IsNullOrEmpty(parentUuid) ? null : FileId.FromUuid(parentUuid);
        }

        private object GetByPhysicalPath(string physicalPath, Fields fields)
        {
            if (string.IsNullOrEmpty(physicalPath)) {
                throw new ApiArgumentException(_physicalPathKey);
            }

            physicalPath = System.Environment.ExpandEnvironmentVariables(physicalPath);

            if (!PathUtil.IsPathRooted(physicalPath)) {
                throw new ApiArgumentException(_physicalPathKey);
            }

            try {
                physicalPath = PathUtil.GetFullPath(physicalPath);
            }
            catch (ArgumentException) {
                throw new ApiArgumentException(_physicalPathKey);
            }

            IFileInfo info = null;

            if (_provider.FileExists(physicalPath)) {
                info = _provider.GetFile(physicalPath);
            }
            else if (_provider.DirectoryExists(physicalPath)) {
                info = _provider.GetDirectory(physicalPath);
            }

            if (info == null) {
                return NotFound();
            }

            return _helper.ToJsonModel(info, fields);
        }

        private IEnumerable<IFileInfo> GetChildren(string physicalPath, string nameFilter)
        {
            var infos = new List<IFileInfo>();

            //
            // Directories
            foreach (var d in _provider.GetDirectories(physicalPath, $"*{nameFilter}*")) {
                if (!d.Attributes.HasFlag(FileAttributes.Hidden) && !d.Attributes.HasFlag(FileAttributes.System)) {
                    infos.Add(d);
                }
            }

            //
            // Files
            foreach (var f in _provider.GetFiles(physicalPath, $"*{nameFilter}*")) {
                if (!f.Attributes.HasFlag(FileAttributes.Hidden) && !f.Attributes.HasFlag(FileAttributes.System)) {
                    infos.Add(f);
                }
            }
            
            if (Context.Request.Headers.ContainsKey(HeaderNames.Range)) {
                long start, finish;

                if (!Context.Request.Headers.TryGetRange(out start, out finish, infos.Count, _units)) {
                    throw new InvalidRangeException();
                }

                Context.Response.Headers.SetContentRange(start, finish, infos.Count);

                return infos.Where((c, index) => index >= start && index <= finish);
            }

            return infos;
        }

        private IEnumerable<IFileInfo> GetFromLocations(string nameFilter)
        {
            var dirs = new List<IFileInfo>();

            foreach (var location in _options.Locations) {
                if (_provider.IsAccessAllowed(location.Path, FileAccess.Read) && 
                        (string.IsNullOrEmpty(nameFilter) || PathUtil.GetName(location.Path).IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) != -1)) {

                    dirs.Add(_provider.GetDirectory(location.Path));
                }
            }

            if (Context.Request.Headers.ContainsKey(HeaderNames.Range)) {
                long start, finish;

                if (!Context.Request.Headers.TryGetRange(out start, out finish, dirs.Count, _units)) {
                    throw new InvalidRangeException();
                }

                Context.Response.Headers.SetContentRange(start, finish, dirs.Count);

                return dirs.Where((c, index) => index >= start && index <= finish);
            }

            return dirs;
        }

        private object PatchFile(dynamic model, FileId fileId)
        {
            var physicalPath = _helper.UpdateFile(model, fileId.PhysicalPath);

            return _helper.ToJsonModel(_provider.GetFile(physicalPath));
        }

        private object PatchDirectory(dynamic model, FileId fileId)
        {
            var physicalPath = _helper.UpdateDirectory(model, fileId.PhysicalPath);

            return _helper.ToJsonModel(_provider.GetDirectory(physicalPath));
        }
    }
}
