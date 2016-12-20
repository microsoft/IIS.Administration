// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Files
{
    using Administration.Files;
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Sites;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
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
            string nameFilter = Context.Request.Query["name"];
            string pathFilter = Context.Request.Query["path"];

            if (!string.IsNullOrEmpty(nameFilter)) {
                if (nameFilter.IndexOfAny(PathUtil.InvalidFileNameChars) != -1) {
                    throw new ApiArgumentException("name");
                }
            }

            if (pathFilter != null) {
                return GetByPath(pathFilter == string.Empty ? "/" : pathFilter);
            }

            return GetByParent(parentUuid, nameFilter);
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



        private object GetByParent(string parentUuid, string nameFilter)
        {
            if (string.IsNullOrEmpty(parentUuid)) {
                return NotFound();
            }

            var fileId = new FileId(parentUuid);

            Site site = SiteHelper.GetSite(fileId.SiteId);

            if (site == null) {
                return NotFound();
            }

            string physicalPath = FilesHelper.GetPhysicalPath(site, fileId.Path);

            if (!_fileService.DirectoryExists(physicalPath)) {
                return NotFound();
            }

            var fields = Context.Request.GetFields();
            var dirInfo = _fileService.GetDirectoryInfo(physicalPath);

            var files = new Dictionary<string, object>();

            //
            // Virtual Directories
            foreach (var vdir in FilesHelper.GetVdirs(site, fileId.Path)) {
                if (string.IsNullOrEmpty(nameFilter) || vdir.Name.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) != -1) {
                    files.Add(vdir.Path, FilesHelper.VdirToJsonModelRef(vdir, fields));
                }
            }

            //
            // Directories
            foreach (var d in dirInfo.GetDirectories(string.IsNullOrEmpty(nameFilter) ? "*" : $"*{nameFilter}*")) {
                string p = Path.Combine(fileId.Path, d.Name);

                if (!files.ContainsKey(p)) {
                    files.Add(p, FilesHelper.DirectoryToJsonModelRef(site, p, fields));
                }
            }

            //
            // Files
            foreach (var f in dirInfo.GetFiles(string.IsNullOrEmpty(nameFilter) ? "*" : $"*{nameFilter}*")) {
                string p = Path.Combine(fileId.Path, f.Name);
                files.Add(p, FilesHelper.FileToJsonModelRef(site, p, fields));
            }

            // Set HTTP header for total count
            this.Context.Response.SetItemsCount(files.Count());

            return new
            {
                files = files.Values
            };
        }

        private object GetByPath(string path)
        {
            if (!FilesHelper.IsValidPath(path)) {
                throw new ApiArgumentException("path");
            }

            Site site = SiteHelper.ResolveSite();

            if (site == null) {
                return NotFound();
            }

            string physicalPath = FilesHelper.GetPhysicalPath(site, path);

            var fields = Context.Request.GetFields();
            var dirInfo = _fileService.GetDirectoryInfo(physicalPath);

            if (!_fileService.DirectoryExists(physicalPath) && !_fileService.FileExists(physicalPath)) {
                throw new NotFoundException("path");
            }

            return FilesHelper.ToJsonModel(site, path, fields);
        }
    }
}
