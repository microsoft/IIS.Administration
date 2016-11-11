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
            foreach (var d in FilesHelper.GetChildVirtualDirectories(site, fileId.Path)) {
                files.Add(FilesHelper.VirtualDirectoryToJsonModelRef(site, d.Application.Path.TrimEnd('/') + d.VirtualDirectory.Path));
            }

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
                case FileType.VirtualDirectory:
                    return FilesHelper.VirtualDirectoryToJsonModel(site, fileId.Path);
                default: throw new NotImplementedException();
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
            // Creating a a file instance requires referencing the target feature
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

            if (name == null) {
                throw new ApiArgumentException("model.name");
            }
            if (type == null) {
                throw new ApiArgumentException("model.type");
            }

            FileType fileType;
            if(!Enum.TryParse(type, true, out fileType) || fileType == FileType.VirtualDirectory) {
                throw new ApiArgumentException("model.type");
            }

            if (fileType == FileType.File) {
                // TODO sanitize name, no invalid file name characters
                File.Create(Path.Combine(physicalPath, name)).Dispose();
            }
            else {
                Directory.CreateDirectory(Path.Combine(physicalPath, name));
            }

            return fileType == FileType.File ? FilesHelper.FileToJsonModel(site, Path.Combine(fileId.Path, name)) : FilesHelper.DirectoryToJsonModel(site, Path.Combine(fileId.Path, name));
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
                case FileType.VirtualDirectory:
                        break;
                    default:
                        break;
                }
            }
            return new NoContentResult();
        }
    }
}
