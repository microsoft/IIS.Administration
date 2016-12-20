// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Core.Utils;
    using Newtonsoft.Json.Linq;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;

    public class CopyController : ApiBaseController
    {
        private IFileProvider _fileService;

        public CopyController()
        {
            _fileService = FileProvider.Default;
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
        public async Task<object> Post([FromBody] dynamic model)
        {
            string name;
            FileId fileId, parentId;

            EnsurePostModelValid(model, out name, out fileId, out parentId);

            if (!_fileService.FileExists(fileId.PhysicalPath)) {
                throw new NotFoundException("file");
            }
            if (!_fileService.DirectoryExists(parentId.PhysicalPath)) {
                throw new NotFoundException("parent");
            }

            var src = new FileInfo(fileId.PhysicalPath);
            var dest = new DirectoryInfo(parentId.PhysicalPath);

            string destPath = name == null ? Path.Combine(dest.FullName, src.Name) : Path.Combine(dest.FullName, name);

            await _fileService.CopyFile(src.FullName, destPath);

            var newFileId = FileId.FromPhysicalPath(destPath);
            FileInfo newFile = _fileService.GetFileInfo(destPath);

            return Created(FilesHelper.GetLocation(newFileId.Uuid), FilesHelper.FileToJsonModel(newFile));
        }



        private void EnsurePostModelValid(dynamic model, out string name, out FileId fileId, out FileId parentId)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            //
            // file
            if (model.file == null) {
                throw new ApiArgumentException("file");
            }
            if (!(model.file is JObject)) {
                throw new ApiArgumentException("file", ApiArgumentException.EXPECTED_OBJECT);
            }
            string fileUuid = DynamicHelper.Value(model.file.id);
            if (fileUuid == null) {
                throw new ApiArgumentException("file.id");
            }

            //
            // parent
            if (model.parent == null) {
                throw new ApiArgumentException("parent");
            }
            if (!(model.parent is JObject)) {
                throw new ApiArgumentException("parent", ApiArgumentException.EXPECTED_OBJECT);
            }
            string parentUuid = DynamicHelper.Value(model.parent.id);
            if (parentUuid == null) {
                throw new ApiArgumentException("parent.id");
            }

            //
            // name
            name = DynamicHelper.Value(model.name);
            if (string.IsNullOrEmpty(name) || Path.IsPathRooted(name)) {
                throw new ApiArgumentException("name");
            }

            fileId = FileId.FromUuid(fileUuid);
            parentId = FileId.FromUuid(parentUuid);
        }
    }
}
