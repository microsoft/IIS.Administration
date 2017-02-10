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

        public async Task CopyDirectory(string sourcePath, string destPath, Func<string, string, Task> copyFile)
        {
            var source = _fileService.GetDirectory(sourcePath);
            var dest = _fileService.GetDirectory(destPath);
            var destParent = dest.Parent;

            if (PathUtil.IsAncestor(sourcePath, destPath) || source.Path.Equals(dest.Path, StringComparison.OrdinalIgnoreCase)) {
                throw new InvalidOperationException();
            }

            if (!source.Exists) {
                throw new DirectoryNotFoundException(source.Path);
            }
            if (destParent == null) {
                throw new IOException(dest.Path);
            }

            if (!dest.Exists) {
                _fileService.CreateDirectory(dest.Path);
            }

            var tasks = new List<Task>();
            var children = Directory.EnumerateDirectories(source.Path, "*", SearchOption.AllDirectories);

            foreach (string dirPath in children) {
                var relative = dirPath.Substring(source.Path.Length).TrimStart(PathUtil.SEPARATORS);
                var destDirPath = Path.Combine(dest.Path, relative);

                tasks.Add(Task.Run(() => {
                    if (!_fileService.DirectoryExists(destDirPath)) {
                        _fileService.CreateDirectory(destDirPath);
                    }

                    foreach (string filePath in Directory.EnumerateFiles(dirPath)) {
                        tasks.Add(copyFile(filePath, Path.Combine(destDirPath, PathUtil.GetName(filePath))));
                    }
                }));
            }

            foreach (var filePath in Directory.EnumerateFiles(source.Path)) {
                tasks.Add(copyFile(filePath, Path.Combine(dest.Path, PathUtil.GetName(filePath))));
            }

            await Task.WhenAll(tasks);
        }

        public MoveOperation Move(IFileInfo source, string dest, bool copy)
        {
            _fileService.EnsureAccess(source.Path, copy ? FileAccess.Read : FileAccess.ReadWrite);
            _fileService.EnsureAccess(dest, FileAccess.Write);

            if (source.Type == FileType.File) {
                return MoveFile(source, dest, copy);
            }
            else if (source.Type == FileType.Directory) {
                return MoveDirectory(source, dest, copy);
            }

            throw new InvalidOperationException();
        }

        private MoveOperation MoveDirectory(IFileInfo source, string dest, bool copy)
        {
            Task t;
            MoveOperation op = null;

            if (copy) {
                t = CopyDirectory(source.Path, dest, (s, d) => SafeMoveFile(s, d, PathUtil.GetTempFilePath(d), true).ContinueWith(t2 => {
                    if (op != null) {
                        op.CurrentSize += _fileService.GetFile(d).Size;
                    }
                }));
            }
            else {
                t = Task.Run(() => _fileService.Move(source.Path, dest));
            }

            op = new MoveOperation(t, source, dest, null, _fileService);
            return op;
        }

        private MoveOperation MoveFile(IFileInfo source, string dest, bool copy)
        {
            string temp = PathUtil.GetTempFilePath(dest);

            Task t = SafeMoveFile(source.Path, dest, temp, copy);

            return new MoveOperation(t, source, dest, temp, _fileService);
        }

        private async Task SafeMoveFile(string source, string dest, string temp, bool copy)
        {
            string swapPath = null;
            try {
                if (copy) {
                    await _fileService.Copy(source, temp);
                }
                else {
                    await Task.Run(() => _fileService.Move(source, temp));
                }

                if (_fileService.FileExists(dest)) {
                    swapPath = PathUtil.GetTempFilePath(dest);
                    _fileService.Move(dest, swapPath);
                }

                _fileService.Move(temp, dest);
            }
            finally {
                if (_fileService.FileExists(temp)) {
                    _fileService.Delete(temp);
                }
                if (swapPath != null && _fileService.FileExists(swapPath) && _fileService.FileExists(dest)) {
                    _fileService.Delete(swapPath);
                }
            }
        }
    }
}