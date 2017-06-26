// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using Core;
    using Core.Utils;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO;
    using System.Threading.Tasks;

    sealed class MoveHelper
    {
        private IFileProvider _fileService;
        private FilesHelper _filesHelper;

        public MoveHelper(IFileProvider fileService)
        {
            _fileService = fileService;
            _filesHelper = new FilesHelper(_fileService);
        }

        public static string GetLocation(string id, bool copy)
        {
            return copy ? $"/{Defines.COPY_PATH}/{id}" : $"/{Defines.MOVE_PATH}/{id}";
        }

        public object ToJsonModel(MoveOperation move)
        {
            dynamic obj = new ExpandoObject();

            obj.id = move.Id;
            obj.status = "running";
            obj.created = move.Created;
            obj.current_size = move.CurrentSize;
            obj.total_size = move.TotalSize;
            obj.file = _filesHelper.ToJsonModelRef(move.Destination);

            return Core.Environment.Hal.Apply(Defines.CopyResource.Guid, obj);
        }

        public void EnsurePostModelValid(dynamic model, out string name, out FileId fileId, out FileId parentId)
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
            if (!string.IsNullOrEmpty(name) && !PathUtil.IsValidFileName(name)) {
                throw new ApiArgumentException("name");
            }

            fileId = FileId.FromUuid(fileUuid);
            parentId = FileId.FromUuid(parentUuid);
        }

        public async Task CopyDirectory(IFileInfo source, IFileInfo dest, Func<IFileInfo, IFileInfo, Task> copyFile)
        {
            var destParent = dest.Parent;

            if (PathUtil.IsAncestor(source.Path, dest.Path) || source.Path.Equals(dest.Path, StringComparison.OrdinalIgnoreCase)) {
                throw new InvalidOperationException();
            }

            if (!source.Exists) {
                throw new DirectoryNotFoundException(source.Path);
            }
            if (destParent == null) {
                throw new IOException(dest.Path);
            }

            if (!dest.Exists) {
                _fileService.CreateDirectory(dest);
            }

            var dirTasks = new List<Task>();
            var fileTasks = new List<Task>();
            var children = Directory.EnumerateDirectories(source.Path, "*", SearchOption.AllDirectories);

            foreach (string dirPath in children) {
                var relative = dirPath.Substring(source.Path.Length).TrimStart(PathUtil.SEPARATORS);
                var destDirPath = Path.Combine(dest.Path, relative);
                IFileInfo destDir = _fileService.GetDirectory(destDirPath);

                dirTasks.Add(Task.Run(() => {
                    if (!destDir.Exists) {
                        _fileService.CreateDirectory(destDir);
                    }

                    foreach (string filePath in Directory.EnumerateFiles(dirPath)) {
                        fileTasks.Add(copyFile(_fileService.GetFile(filePath),
                                           _fileService.GetFile(Path.Combine(destDirPath, PathUtil.GetName(filePath)))));
                    }
                }));
            }

            foreach (var filePath in Directory.EnumerateFiles(source.Path)) {
                fileTasks.Add(copyFile(_fileService.GetFile(filePath),
                                   _fileService.GetFile(Path.Combine(dest.Path, PathUtil.GetName(filePath)))));
            }

            await Task.WhenAll(dirTasks);
            await Task.WhenAll(fileTasks);
        }

        public MoveOperation Move(IFileInfo source, string dest, bool copy)
        {
            _fileService.EnsureAccess(source.Path, copy ? FileAccess.Read : FileAccess.ReadWrite);
            _fileService.EnsureAccess(dest, FileAccess.Write);

            IFileInfo destination = null;

            if (source.Type == FileType.File) {
                destination = _fileService.GetFile(dest);
                return MoveFile(source, destination, copy);
            }
            else if (source.Type == FileType.Directory) {
                destination = _fileService.GetDirectory(dest);
                return MoveDirectory(source, destination, copy);
            }

            throw new InvalidOperationException();
        }

        private MoveOperation MoveDirectory(IFileInfo source, IFileInfo destination, bool copy)
        {
            var op = new MoveOperation(source, destination, null, _fileService);

            if (copy) {
                op.Task = CopyDirectory(source, destination, (s, d) => SafeMoveFile(s, d, PathUtil.GetTempFilePath(d.Path), true).ContinueWith(t2 => {
                    if (op != null) {
                        op.CurrentSize += _fileService.GetFile(d.Path).Size;
                    }
                }));
            }
            else {
                op.Task = Task.Run(() => _fileService.Move(source, destination));
            }

            return op;
        }

        private MoveOperation MoveFile(IFileInfo source, IFileInfo destination, bool copy)
        {
            string temp = PathUtil.GetTempFilePath(destination.Path);

            var op = new MoveOperation(source, destination, temp, _fileService);

            op.Task = SafeMoveFile(source, destination, temp, copy);

            return op;
        }

        private async Task SafeMoveFile(IFileInfo source, IFileInfo destination, string temp, bool copy)
        {
            IFileInfo swapInfo = null;
            IFileInfo tempInfo = _fileService.GetFile(temp);
            try {
                if (copy) {
                    await _fileService.Copy(source, tempInfo);
                }
                else {
                    await Task.Run(() => _fileService.Move(source, tempInfo));
                }

                if (destination.Exists) {
                    swapInfo = _fileService.GetFile(PathUtil.GetTempFilePath(destination.Path));
                    _fileService.Move(destination, swapInfo);
                }

                _fileService.Move(tempInfo, destination);
            }
            finally {
                // Refresh
                tempInfo = _fileService.GetFile(temp);
                if (swapInfo != null) {
                    swapInfo = _fileService.GetFile(swapInfo.Path);
                    destination = _fileService.GetFile(destination.Path);
                }

                if (tempInfo.Exists) {
                    _fileService.Delete(tempInfo);
                }

                if (swapInfo != null && swapInfo.Exists && destination.Exists) {
                    _fileService.Delete(swapInfo);
                }
            }
        }
    }
}