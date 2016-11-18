// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using Core;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;

    public class FileService
    {
        private static AccessControl _accessControl = new AccessControl();

        public string GetName(string path)
        {
            var info = new FileInfo(path);
            return info.Name;
        }

        public string GetParentPath(string path)
        {
            var info = new FileInfo(path);
            return info.Directory?.FullName;
        }

        public Stream GetFile(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            if (!_accessControl.IsAccessAllowed(path, fileAccess)) {
                throw new ForbiddenException(path);
            }

            return new FileStream(path, fileMode, fileAccess, fileShare);
        }

        public FileInfo GetFileInfo(string path)
        {
            if (!_accessControl.IsAccessAllowed(path, FileAccess.Read)) {
                throw new ForbiddenException(path);
            }

            return new FileInfo(path);
        }

        public FileVersionInfo GetFileVersionInfo(string path)
        {
            if (!_accessControl.IsAccessAllowed(path, FileAccess.Read)) {
                throw new ForbiddenException(path);
            }

            return FileVersionInfo.GetVersionInfo(path);
        }

        public DirectoryInfo GetDirectoryInfo(string path)
        {
            if (!_accessControl.IsAccessAllowed(path, FileAccess.Read)) {
                throw new ForbiddenException(path);
            }

            return new DirectoryInfo(path);
        }

        public async Task CopyFile(string sourcePath, string destPath, bool copyMetadata = false)
        {
            if (!_accessControl.IsAccessAllowed(sourcePath, FileAccess.ReadWrite)) {
                throw new ForbiddenException(sourcePath);
            }
            if (!_accessControl.IsAccessAllowed(destPath, FileAccess.ReadWrite)) {
                throw new ForbiddenException(destPath);
            }

            using (var srcStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                using (var destStream = new FileStream(destPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read)) {
                    await srcStream.CopyToAsync(destStream);
                }
            }

            if (copyMetadata) {
                var sourceFileInfo = new FileInfo(sourcePath);
                var destFileInfo = new FileInfo(destPath);

                destFileInfo.CreationTime = sourceFileInfo.CreationTime;
                destFileInfo.CreationTimeUtc = sourceFileInfo.CreationTimeUtc;
            }
        }

        public void MoveFile(string sourcePath, string destPath)
        {
            if (!_accessControl.IsAccessAllowed(sourcePath, FileAccess.ReadWrite)) {
                throw new ForbiddenException(sourcePath);
            }
            if (!_accessControl.IsAccessAllowed(destPath, FileAccess.ReadWrite)) {
                throw new ForbiddenException(destPath);
            }

            File.Move(sourcePath, destPath);
        }

        public void MoveDirectory(string sourcePath, string destPath)
        {
            if (!_accessControl.IsAccessAllowed(sourcePath, FileAccess.ReadWrite)) {
                throw new ForbiddenException(sourcePath);
            }
            if (!_accessControl.IsAccessAllowed(destPath, FileAccess.ReadWrite)) {
                throw new ForbiddenException(destPath);
            }

            Directory.Move(sourcePath, destPath);
        }

        public void DeleteFile(string path)
        {
            if (!_accessControl.IsAccessAllowed(path, FileAccess.ReadWrite)) {
                throw new ForbiddenException(path);
            }

            File.Delete(path);
        }

        public void DeleteDirectory(string path)
        {
            if (!_accessControl.IsAccessAllowed(path, FileAccess.ReadWrite)) {
                throw new ForbiddenException(path);
            }

            Directory.Delete(path, true);
        }

        public FileInfo CreateFile(string path)
        {
            if (!_accessControl.IsAccessAllowed(path, FileAccess.ReadWrite)) {
                throw new ForbiddenException(path);
            }

            File.Create(path).Dispose();
            return new FileInfo(path);
        }

        public DirectoryInfo CreateDirectory(string path)
        {
            if (!_accessControl.IsAccessAllowed(path, FileAccess.ReadWrite)) {
                throw new ForbiddenException(path);
            }

            Directory.CreateDirectory(path);
            return new DirectoryInfo(path);
        }

        public bool FileExists(string path)
        {
            if (!_accessControl.IsAccessAllowed(path, FileAccess.Read)) {
                throw new ForbiddenException(path);
            }

            return File.Exists(path);
        }

        public bool DirectoryExists(string path)
        {
            if (!_accessControl.IsAccessAllowed(path, FileAccess.Read)) {
                throw new ForbiddenException(path);
            }

            return Directory.Exists(path);
        }
    }
}
