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
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using Web.Administration;

    public class WsFilesController : ApiBaseController
    {
        private IFileProvider _fileService;

        public WsFilesController()
        {
            _fileService = FileProvider.Default;
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.FilesName)]
        public object Get()
        {
            string parentUuid = Context.Request.Query[Defines.PARENT_IDENTIFIER];
            string name = Context.Request.Query["name"];
            string path = Context.Request.Query["path"];

            if (!string.IsNullOrEmpty(name)) {
                if (name.IndexOfAny(Path.GetInvalidFileNameChars()) != -1) {
                    throw new ApiArgumentException("name");
                }
            }

            Site site = null;

            if (path != null) {
                site = SiteHelper.ResolveSite();
            }
            else {
                if (string.IsNullOrEmpty(parentUuid)) {
                    return NotFound();
                }

                var fileId = new FileId(parentUuid);

                site = SiteHelper.GetSite(fileId.SiteId);
                path = fileId.Path;
            }

            if (site == null) {
                return NotFound();
            }

            string physicalPath = FilesHelper.GetPhysicalPath(site, path);
            if (!_fileService.DirectoryExists(physicalPath)) {
                return NotFound();
            }

            var fields = Context.Request.GetFields();
            var dirInfo = _fileService.GetDirectoryInfo(physicalPath);

            var files = new Dictionary<string, object>();

            //
            // Virtual Directories
            foreach (var vdir in FilesHelper.GetVdirs(site, path)) {
                if (string.IsNullOrEmpty(name) || vdir.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) != -1) {
                    files.Add(vdir.Path, FilesHelper.VdirToJsonModelRef(vdir, fields));
                }
            }

            //
            // Directories
            foreach (var d in dirInfo.GetDirectories(string.IsNullOrEmpty(name) ? "*" : $"*{name}*")) {
                string p = Path.Combine(path, d.Name);

                if (!files.ContainsKey(p)) {
                    files.Add(p, FilesHelper.DirectoryToJsonModelRef(site, p, fields));
                }
            }

            //
            // Files
            foreach (var f in dirInfo.GetFiles(string.IsNullOrEmpty(name) ? "*" : $"*{name}*")) {
                string p = Path.Combine(path, f.Name);
                files.Add(p, FilesHelper.FileToJsonModelRef(site, p, fields));
            }

            // Set HTTP header for total count
            this.Context.Response.SetItemsCount(files.Count());

            return new {
                files = files.Values
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.FileName)]
        public object Get(string id)
        {
            FileId fileId = new FileId(id);
            Site site = SiteHelper.GetSite(fileId.SiteId);

            if (site == null) {
                return NotFound();
            }

            var physicalPath = FilesHelper.GetPhysicalPath(site, fileId.Path);
            
            if (!_fileService.FileExists(physicalPath) && !_fileService.DirectoryExists(physicalPath)) {
                return NotFound();
            }

            var fields = Context.Request.GetFields();

            return FilesHelper.ToJsonModel(site, fileId.Path, fields);
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

            FileId fileId = new FileId(parentUuid);
            Site site = SiteHelper.GetSite(fileId.SiteId);

            if (site == null) {
                return NotFound();
            }

            var physicalPath = FilesHelper.GetPhysicalPath(site, fileId.Path);
            
            if (!_fileService.DirectoryExists(physicalPath)) {
                throw new NotFoundException(physicalPath);
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
            if(type == null || !Enum.TryParse(type, true, out fileType) || fileType == FileType.VDir) {
                throw new ApiArgumentException("model.type");
            }

            var fields = Context.Request.GetFields();

            //
            // Create
            if (fileType == FileType.File) {
                _fileService.CreateFile(Path.Combine(physicalPath, name));
            }
            else {
                _fileService.CreateDirectory(Path.Combine(physicalPath, name));
            }

            dynamic file = FilesHelper.ToJsonModel(site, Path.Combine(fileId.Path, name), fields);

            return Created(FilesHelper.GetLocation(file.id), file);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.FileName)]
        public object Patch([FromBody] dynamic model, string id)
        {
            FileId fileId = new FileId(id);
            Site site = SiteHelper.GetSite(fileId.SiteId);

            if (site == null) {
                return NotFound();
            }

            var physicalPath = FilesHelper.GetPhysicalPath(site, fileId.Path);
            
            if (!_fileService.FileExists(physicalPath) && !_fileService.DirectoryExists(physicalPath)) {
                return NotFound();
            }
            
            dynamic result = null;
            switch (FilesHelper.GetFileType(site, fileId.Path, physicalPath)) {

                case FileType.File:
                    result = PatchFile(model, site, fileId, physicalPath);
                    break;

                case FileType.Directory:
                    result = PatchDirectory(model, site, fileId, physicalPath);
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

            FileId fileId = new FileId(id);
            Site site = SiteHelper.GetSite(fileId.SiteId);
            string physicalPath = null;

            if (site != null) {
                physicalPath = FilesHelper.GetPhysicalPath(site, fileId.Path);
            }

            if (!string.IsNullOrEmpty(physicalPath) && (_fileService.FileExists(physicalPath) || _fileService.DirectoryExists(physicalPath))) {
                switch (FilesHelper.GetFileType(site, fileId.Path, physicalPath)) {
                    case FileType.File:
                        _fileService.DeleteFile(physicalPath);
                        break;
                    case FileType.Directory:
                        _fileService.DeleteDirectory(physicalPath);
                        break;
                    case FileType.VDir:
                        break;
                    default:
                        break;
                }
            }
            return new NoContentResult();
        }



        private object PatchFile(dynamic model, Site site, FileId fileId, string physicalPath)
        {
            var fields = Context.Request.GetFields();
            var OldFName = _fileService.GetName(physicalPath);

            physicalPath = FilesHelper.UpdateFile(model, physicalPath);

            var newPath = fileId.Path.Substring(0, fileId.Path.LastIndexOf(OldFName)) + _fileService.GetName(physicalPath);

            return FilesHelper.FileToJsonModel(site, newPath, fields);
        }

        private object PatchDirectory(dynamic model, Site site, FileId fileId, string physicalPath)
        {
            var fields = Context.Request.GetFields();
            var oldDirName = _fileService.GetName(physicalPath);

            physicalPath = FilesHelper.UpdateDirectory(model, physicalPath);

            var newPath = fileId.Path.Substring(0, fileId.Path.LastIndexOf(oldDirName)) + _fileService.GetName(physicalPath);

            return FilesHelper.DirectoryToJsonModel(site, newPath, fields);
        }
    }
}
