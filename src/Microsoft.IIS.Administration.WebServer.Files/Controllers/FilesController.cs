// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Files
{
    using Core.Http;
    using AspNetCore.Mvc;
    using Sites;
    using System.IO;
    using Web.Administration;
    using System.Collections.Generic;
    using System;
    using Core;
    using Newtonsoft.Json.Linq;
    using Core.Utils;
    using Administration.Files;

    public class FilesController : ApiBaseController
    {
        [HttpGet]
        public object Get()
        {
            string parentUuid = Context.Request.Query[Defines.PARENT_IDENTIFIER];

            if (string.IsNullOrEmpty(parentUuid)) {
                return NotFound();
            }

            var fileId = new FileId(parentUuid);
            
            Site site = SiteHelper.GetSite(fileId.SiteId);

            if (site == null) {
                return NotFound();
            }

            string physicalPath = FilesHelper.GetPhysicalPath(site, fileId.Path);
            if (!Directory.Exists(physicalPath)) {
                return NotFound();
            }

            var files = new List<object>();
            var dInfo = new DirectoryInfo(physicalPath);            

            // Files & directories
            foreach (var f in dInfo.GetFiles()) {
                files.Add(FilesHelper.FileToJsonModelRef(site, Path.Combine(fileId.Path, f.Name)));
            }
            foreach (var d in dInfo.GetDirectories()) {
                files.Add(FilesHelper.DirectoryToJsonModelRef(site, Path.Combine(fileId.Path, d.Name)));
            }
            
            // Virtual Directories
            foreach (var fullVdir in FilesHelper.GetChildVirtualDirectories(site, fileId.Path)) {
                files.Add(FilesHelper.VirtualDirectoryToJsonModelRef(fullVdir));
            }

            // Set HTTP header for total count
            this.Context.Response.SetItemsCount(files.Count);

            return new {
                files = files
            };
        }

        [HttpGet]
        public object Get(string id)
        {
            FileId fileId = new FileId(id);
            Site site = SiteHelper.GetSite(fileId.SiteId);

            if (site == null) {
                return NotFound();
            }

            var physicalPath = FilesHelper.GetPhysicalPath(site, fileId.Path);

            if (!File.Exists(physicalPath) && !Directory.Exists(physicalPath)) {
                return NotFound();
            }

            switch (FilesHelper.GetFileType(site, fileId.Path, physicalPath)) {
                case FileType.File:
                    return FilesHelper.FileToJsonModel(site, fileId.Path);
                case FileType.Directory:
                    return FilesHelper.DirectoryToJsonModel(site, fileId.Path);
                case FileType.VDir:
                    return FilesHelper.VirtualDirectoryToJsonModel(FilesHelper.ResolveFullVdir(site, fileId.Path));
                default:
                    return null;
            }
        }

        [HttpPost]
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
            // Creating a file instance requires referencing the target feature
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

            if (!Directory.Exists(physicalPath)) {
                return NotFound();
            }

            string name = DynamicHelper.Value(model.name);
            string type = DynamicHelper.Value(model.type);

            // Here we make sure the file name is safe. No '..', no invalid characters
            if (name == null || !FilesHelper.IsValidFileName(name)) {
                throw new ApiArgumentException("model.name");
            }
            if (type == null) {
                throw new ApiArgumentException("model.type");
            }

            FileType fileType;
            if(!Enum.TryParse(type, true, out fileType) || fileType == FileType.VDir) {
                throw new ApiArgumentException("model.type");
            }
            
            if (fileType == FileType.File) {
                File.Create(Path.Combine(physicalPath, name)).Dispose();
            }
            else {
                Directory.CreateDirectory(Path.Combine(physicalPath, name));
            }

            return FilesHelper.GetFileType(site, fileId.Path, physicalPath) == FileType.File ? FilesHelper.FileToJsonModel(site, Path.Combine(fileId.Path, name)) : FilesHelper.DirectoryToJsonModel(site, Path.Combine(fileId.Path, name));
        }

        [HttpPatch]
        public object Patch([FromBody] dynamic model, string id)
        {
            FileId fileId = new FileId(id);
            Site site = SiteHelper.GetSite(fileId.SiteId);

            if (site == null) {
                return NotFound();
            }

            var physicalPath = FilesHelper.GetPhysicalPath(site, fileId.Path);

            if (!File.Exists(physicalPath) && !Directory.Exists(physicalPath)) {
                return NotFound();
            }

            switch (FilesHelper.GetFileType(site, fileId.Path, physicalPath)) {

                case FileType.File:
                    var fileInfo = new FileInfo(physicalPath);
                    var OldFName = fileInfo.Name;

                    FilesHelper.UpdateFile(model, fileInfo);

                    dynamic f = FilesHelper.FileToJsonModel(site, fileId.Path.Substring(0, fileId.Path.LastIndexOf(OldFName)) + fileInfo.Name);

                    if (f.id != id) {
                        return LocationChanged(FilesHelper.GetLocation(f.id), f);
                    }

                    return f;

                case FileType.Directory:
                    var dirInfo = new DirectoryInfo(physicalPath);
                    var oldDirName = dirInfo.Name;

                    FilesHelper.UpdateDirectory(model, dirInfo);

                    dynamic d = FilesHelper.DirectoryToJsonModel(site, fileId.Path.Substring(0, fileId.Path.LastIndexOf(oldDirName)) + dirInfo.Name);

                    if (d.id != id) {
                        return LocationChanged(FilesHelper.GetLocation(d.id), d);
                    }

                    return d;

                case FileType.VDir:
                    return FilesHelper.VirtualDirectoryToJsonModel(FilesHelper.ResolveFullVdir(site, fileId.Path));

                default:
                    return null;
            }
        }

        [HttpDelete]
        public IActionResult Delete(string id)
        {

            FileId fileId = new FileId(id);
            Site site = SiteHelper.GetSite(fileId.SiteId);
            string physicalPath = null;

            if (site != null) {
                physicalPath = FilesHelper.GetPhysicalPath(site, fileId.Path);
            }

            if (!string.IsNullOrEmpty(physicalPath) && (File.Exists(physicalPath) || Directory.Exists(physicalPath))) {
                switch (FilesHelper.GetFileType(site, fileId.Path, physicalPath)) {
                    case FileType.File:
                        File.Delete(physicalPath);
                        break;
                    case FileType.Directory:
                        Directory.Delete(physicalPath);
                        break;
                    case FileType.VDir:
                        break;
                    default:
                        break;
                }
            }
            return new NoContentResult();
        }
    }
}
