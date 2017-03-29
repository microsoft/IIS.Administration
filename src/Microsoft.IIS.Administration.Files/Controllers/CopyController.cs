// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Net;

    public class CopyController : ApiBaseController
    {
        private MoveHelper _helper;
        private IFileProvider _fileService;
        private static ConcurrentDictionary<string, MoveOperation> _copies = new ConcurrentDictionary<string, MoveOperation>();

        public CopyController(IFileProvider fileProvider)
        {
            _fileService = fileProvider;
            _helper = new MoveHelper(_fileService);
        }
        
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
            IFileInfo src;
            FileId fileId, parentId;

            _helper.EnsurePostModelValid(model, out name, out fileId, out parentId);
            src = new FilesHelper(_fileService).GetExistingFileInfo(fileId.PhysicalPath);

            if (src == null) {
                throw new NotFoundException("file");
            }

            if (!_fileService.GetDirectory(parentId.PhysicalPath).Exists) {
                throw new NotFoundException("parent");
            }

            string destPath = Path.Combine(parentId.PhysicalPath, name == null ? src.Name : name);

            if (PathUtil.IsAncestor(src.Path, destPath) || src.Path.Equals(destPath, StringComparison.OrdinalIgnoreCase)) {
                throw new ApiArgumentException("parent", "The destination folder is a subfolder of the source");
            }

            if (src.Type == FileType.File && _fileService.GetDirectory(destPath).Exists || src.Type == FileType.Directory && _fileService.GetFile(destPath).Exists) {
                throw new AlreadyExistsException("name");
            }

            MoveOperation copy = InitiateCopy(src, destPath);

            Context.Response.StatusCode = (int) HttpStatusCode.Accepted;
            Context.Response.Headers.Add("Location", MoveHelper.GetLocation(copy.Id, true));
            
            return _helper.ToJsonModel(copy);
        }

        [HttpGet]
        public object Get(string id)
        {
            MoveOperation copy = null;

            if (!_copies.TryGetValue(id, out copy)) {
                return NotFound();
            }

            return _helper.ToJsonModel(copy);
        }

        private MoveOperation InitiateCopy(IFileInfo source, string destination)
        {
            MoveOperation copy = _helper.Move(source, destination, true);

            _copies.TryAdd(copy.Id, copy);

            var continuation = copy.Task.ContinueWith(t => _copies.TryRemove(copy.Id, out copy));

            return copy;
        }
    }
}
