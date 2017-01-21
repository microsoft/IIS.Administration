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

        public MoveHelper(IFileProvider fileService)
        {
            _fileService = fileService;
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

            if (move.Source is FileInfo && !string.IsNullOrEmpty(move.TempPath)) {
                var sourceFile = (FileInfo)move.Source;
                var tempInfo = new FileInfo(move.TempPath);

                obj.current_size = tempInfo.Exists ? tempInfo.Length : 0;
                obj.total_size = sourceFile.Exists ? sourceFile.Length : 0;
            }

            obj.file = FilesHelper.ToJsonModelRef(move.Destination);

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
            var source = _fileService.GetDirectoryInfo(sourcePath);
            var dest = _fileService.GetDirectoryInfo(destPath);
            var destParent = dest.Parent;

            if (PathUtil.IsAncestor(sourcePath, destPath) || source.FullName.Equals(dest.FullName, StringComparison.OrdinalIgnoreCase)) {
                throw new InvalidOperationException();
            }

            if (!source.Exists) {
                throw new DirectoryNotFoundException(source.FullName);
            }
            if (destParent == null) {
                throw new IOException(dest.FullName);
            }

            if (!dest.Exists) {
                _fileService.CreateDirectory(dest.FullName);
            }

            var tasks = new List<Task>();
            var children = source.EnumerateDirectories("*", SearchOption.AllDirectories);

            foreach (DirectoryInfo dir in children) {
                var relative = dir.FullName.Substring(source.FullName.Length).TrimStart(PathUtil.SEPARATORS);
                var destDirPath = Path.Combine(dest.FullName, relative);

                tasks.Add(Task.Run(() => {
                    if (!_fileService.DirectoryExists(destDirPath)) {
                        _fileService.CreateDirectory(destDirPath);
                    }

                    foreach (FileInfo file in dir.EnumerateFiles()) {
                        tasks.Add(copyFile(file.FullName, Path.Combine(destDirPath, file.Name)));
                    }
                }));
            }

            foreach (var file in source.EnumerateFiles()) {
                tasks.Add(copyFile(file.FullName, Path.Combine(dest.FullName, file.Name)));
            }

            await Task.WhenAll(tasks);
        }

        public MoveOperation Move(FileSystemInfo source, string dest, bool copy)
        {
            FileType type = source is FileInfo ? FileType.File : FileType.Directory;
            
            if (type == FileType.File) {
                return MoveFile((FileInfo)source, dest, copy);
            }
            else {
                return MoveDirectory((DirectoryInfo)source, dest, copy);
            }
        }

        private MoveOperation MoveDirectory(DirectoryInfo source, string dest, bool copy)
        {
            Task t;
            if (copy) {
                t = CopyDirectory(source.FullName, dest, (s, d) => SafeMoveFile(s, d, PathUtil.GetTempFilePath(d), copy));
            }
            else {
                t = Task.Run(() => _fileService.MoveDirectory(source.FullName, dest));
            }
            return new MoveOperation(t, source, dest, null);
        }

        private MoveOperation MoveFile(FileInfo source, string dest, bool copy)
        {
            string temp = PathUtil.GetTempFilePath(dest);

            Task t = SafeMoveFile(source.FullName, dest, temp, copy);

            return new MoveOperation(t, source, dest, temp);
        }

        private async Task SafeMoveFile(string source, string dest, string temp, bool copy)
        {
            string swapPath = null;
            try {
                if (copy) {
                    await _fileService.CopyFile(source, temp);
                }
                else {
                    await Task.Run(() => _fileService.MoveFile(source, temp));
                }

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
    }
}