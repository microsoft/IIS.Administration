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

    public class MoveController : ApiBaseController
    {
        private MoveHelper _helper;
        private IFileProvider _fileService;
        private static ConcurrentDictionary<string, MoveOperation> _moves = new ConcurrentDictionary<string, MoveOperation>();

        public MoveController(IFileProvider fileProvider)
        {
            _fileService = fileProvider;
            _helper = new MoveHelper(_fileService);
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
            FileType fileType;
            FileId fileId, parentId;

            _helper.EnsurePostModelValid(model, out name, out fileId, out parentId);

            try {
                fileType = FilesHelper.GetFileType(fileId.PhysicalPath);
            }
            catch (FileNotFoundException) {
                throw new NotFoundException("file");
            }

            if (!_fileService.DirectoryExists(parentId.PhysicalPath)) {
                throw new NotFoundException("parent");
            }

            var src = fileType == FileType.File ? _fileService.GetFile(fileId.PhysicalPath) : (IFileSystemInfo)_fileService.GetDirectory(fileId.PhysicalPath);

            string destPath = Path.Combine(parentId.PhysicalPath, name == null ? src.Name : name);

            if (PathUtil.IsAncestor(src.Path, destPath) || src.Path.Equals(destPath, StringComparison.OrdinalIgnoreCase)) {
                throw new ApiArgumentException("parent", "The destination folder is a subfolder of the source");
            }

            if (fileType == FileType.File && _fileService.DirectoryExists(destPath) || fileType == FileType.Directory && _fileService.FileExists(destPath)) {
                throw new AlreadyExistsException("name");
            }

            MoveOperation move = InitiateMove(src, destPath);

            Context.Response.StatusCode = (int) HttpStatusCode.Accepted;
            Context.Response.Headers.Add("Location", MoveHelper.GetLocation(move.Id, false));

            return _helper.ToJsonModel(move);
        }

        [HttpGet]
        public object Get(string id)
        {
            MoveOperation move = null;

            if (!_moves.TryGetValue(id, out move)) {
                return NotFound();
            }

            return _helper.ToJsonModel(move);
        }

        private MoveOperation InitiateMove(IFileSystemInfo source, string destination)
        {
            MoveOperation move = _helper.Move(source, destination, false);

            _moves.TryAdd(move.Id, move);

            var continuation = move.Task.ContinueWith(t => _moves.TryRemove(move.Id, out move));

            return move;
        }
    }
}
