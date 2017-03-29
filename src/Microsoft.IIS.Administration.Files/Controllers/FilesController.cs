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
        
        IFileProvider _provider;
        FilesHelper _helper;

        public FilesController(IFileProvider fileProvider)
        {
            _provider = fileProvider;
            _helper = new FilesHelper(fileProvider);
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

            if (parentId != null && !_provider.GetDirectory(parentId.PhysicalPath).Exists) {
                return NotFound();
            }

            IEnumerable<object> children = null;

            if (parentId == null) {
                var list = new List<object>();
                children = list;

                foreach (IFileInfo child in GetFromLocations(nameFilter)) {
                    list.Add(_helper.ToJsonModelRef(child, fields));
                }
            }
            else {
                IFileInfo parent = _provider.GetDirectory(parentId.PhysicalPath);
                children = _helper.DirectoryContentToJsonModel(parent, GetChildren(parent, nameFilter), fields);
            }

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
            
            if (parentId != null && !_provider.GetDirectory(parentId.PhysicalPath).Exists) {
                return NotFound();
            }

            IEnumerable<object> models = null;

            if (parentId == null) {
                var list = new List<object>();
                models = list;

                foreach (IFileInfo child in GetFromLocations(nameFilter)) {
                    list.Add(_helper.ToJsonModelRef(child, fields));
                }
            }
            else {
                IFileInfo parent = _provider.GetDirectory(parentId.PhysicalPath);
                models = _helper.DirectoryContentToJsonModel(parent, GetChildren(parent, nameFilter), fields);
            }

            // Set HTTP header for total count
            this.Context.Response.SetItemsCount(models.Count());
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

            IFileInfo info = _helper.GetExistingFileInfo(fileId.PhysicalPath);

            if (info == null) {
                return NotFound();
            }

            return _helper.ToJsonModel(info);
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

            if (!_provider.GetDirectory(fileId.PhysicalPath).Exists) {
                throw new NotFoundException("parent");
            }

            //
            // Check Name
            string name = DynamicHelper.Value(model.name)?.Trim();

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

            string creationPath = Path.Combine(fileId.PhysicalPath, name);

            if (_provider.GetFile(creationPath).Exists || _provider.GetDirectory(creationPath).Exists) {
                throw new AlreadyExistsException("name");
            }

            IFileInfo info = fileType == FileType.File ? _provider.CreateFile(_provider.GetFile(creationPath))
                                                       : _provider.CreateDirectory(_provider.GetDirectory(creationPath));

            _provider.SetFileTime(info, lastAccess, lastModified, created);

            dynamic file = _helper.ToJsonModel(info);

            return Created(FilesHelper.GetLocation(file.id), _helper.ToJsonModel(info));
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.FileName)]
        public object Patch([FromBody] dynamic model, string id)
        {
            FileId fileId = FileId.FromUuid(id);

            IFileInfo info = _helper.GetExistingFileInfo(fileId.PhysicalPath);

            if (info == null) {
                return NotFound();
            }

            dynamic result = null;
            switch (info.Type) {

                case FileType.File:
                    result = _helper.ToJsonModel(_helper.UpdateFile(model, info));
                    break;

                case FileType.Directory:
                    result = _helper.ToJsonModel(_helper.UpdateDirectory(model, info));
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

            IFileInfo info = _helper.GetExistingFileInfo(fileId.PhysicalPath);

            if (info != null) {
                _provider.Delete(info);
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

            IFileInfo info = _helper.GetExistingFileInfo(physicalPath);

            if (info == null) {
                throw new NotFoundException(_physicalPathKey);
            }

            return _helper.ToJsonModel(info, fields);
        }

        private IEnumerable<IFileInfo> GetChildren(IFileInfo directory, string nameFilter)
        {
            var infos = new List<IFileInfo>();

            //
            // Directories
            foreach (var d in _provider.GetDirectories(directory, $"*{nameFilter}*")) {
                if (!d.Attributes.HasFlag(FileAttributes.Hidden) && !d.Attributes.HasFlag(FileAttributes.System)) {
                    infos.Add(d);
                }
            }

            //
            // Files
            foreach (var f in _provider.GetFiles(directory, $"*{nameFilter}*")) {
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
            var dirs = _provider.GetDirectories(null, nameFilter);

            if (Context.Request.Headers.ContainsKey(HeaderNames.Range)) {
                long start, finish;

                if (!Context.Request.Headers.TryGetRange(out start, out finish, dirs.Count(), _units)) {
                    throw new InvalidRangeException();
                }

                Context.Response.Headers.SetContentRange(start, finish, dirs.Count());

                return dirs.Where((c, index) => index >= start && index <= finish);
            }

            return dirs;
        }
    }
}
