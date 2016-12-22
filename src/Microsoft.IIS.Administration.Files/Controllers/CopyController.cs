// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Core.Utils;
    using Newtonsoft.Json.Linq;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;

    public class CopyController : ApiBaseController
    {
        private IFileProvider _fileService;
        private static ConcurrentDictionary<string, Copy> _copies = new ConcurrentDictionary<string, Copy>();

        public CopyController()
        {
            _fileService = FileProvider.Default;
        }
        
        [HttpHead]
        [HttpPatch]
        [HttpPut]
        public IActionResult NotAllowed()
        {
            return new StatusCodeResult((int)HttpStatusCode.MethodNotAllowed);
        }

        [HttpPost]
        [Audit]
        public object Post([FromBody] dynamic model)
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

            string destPath = Path.Combine(dest.FullName, name == null ? src.Name : name);

            Copy copy = InitiateCopy(src.FullName, destPath);

            Context.Response.StatusCode = (int)HttpStatusCode.Accepted;
            Context.Response.Headers.Add("Location", CopyHelper.GetLocation(copy.Id));

            return CopyHelper.ToJsonModel(copy);
        }

        [HttpGet]
        public object Get(string id)
        {
            Copy copy = null;

            if (!_copies.TryGetValue(id, out copy)) {
                return NotFound();
            }

            return CopyHelper.ToJsonModel(copy);
        }



        private Copy InitiateCopy(string source, string destination)
        {
            string temp = PathUtil.GetTempFilePath(destination);

            var task = SafeCopy(source, destination, temp);

            Copy copy = new Copy(task, source, destination, temp);

            _copies.TryAdd(copy.Id, copy);

            var continuation = task.ContinueWith(t => _copies.TryRemove(copy.Id, out copy));

            return copy;
        }

        private async Task SafeCopy(string source, string dest, string temp)
        {
            string swapPath = null;

            try {
                await _fileService.CopyFile(source, temp);

                if (_fileService.FileExists(dest)) {
                    swapPath = PathUtil.GetTempFilePath(dest);
                    _fileService.MoveFile(dest, swapPath);
                }

                _fileService.MoveFile(temp, dest);
            }
            finally {
                if (_fileService.FileExists(temp)) {
                    _fileService.DeleteFile(temp);
                }
                if (swapPath != null && _fileService.FileExists(swapPath) && _fileService.FileExists(dest)) {
                    _fileService.DeleteFile(swapPath);
                }
            }
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
            if (string.IsNullOrEmpty(name) || !PathUtil.IsValidFileName(name) || Path.IsPathRooted(name)) {
                throw new ApiArgumentException("name");
            }

            fileId = FileId.FromUuid(fileUuid);
            parentId = FileId.FromUuid(parentUuid);
        }
    }
}
