// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using Core;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;

    public class FileProvider : IFileProvider
    {
        private const string PATH_UNLISTED = "Access Denied";

        private IAccessControl _accessControl;

        public FileProvider(IAccessControl accessControl)
        {
            if (accessControl == null) {
                throw new ArgumentNullException(nameof(accessControl));
            }

            _accessControl = accessControl;
        }

        public static IFileProvider Default { get; } = new FileProvider(AccessControl.Default);

        public string GetName(string path)
        {
            EnsureAccess(path, FileAccess.Read);

            var info = new FileInfo(path);
            return info.Name;
        }

        public string GetParentPath(string path)
        {
            EnsureAccess(path, FileAccess.Read);

            var info = new FileInfo(path);
            return info.Directory?.FullName;
        }

        public Stream GetFile(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            EnsureAccess(path, fileAccess);

            return PerformIO(p => new FileStream(p, fileMode, fileAccess, fileShare), path);
        }

        public FileInfo GetFileInfo(string path)
        {
            EnsureAccess(path, FileAccess.Read);

            return PerformIO(p => new FileInfo(p), path);
        }

        public FileVersionInfo GetFileVersionInfo(string path)
        {
            EnsureAccess(path, FileAccess.Read);

            return PerformIO(p => FileVersionInfo.GetVersionInfo(p), path);
        }

        public DirectoryInfo GetDirectoryInfo(string path)
        {
            EnsureAccess(path, FileAccess.Read);

            return new DirectoryInfo(path);
        }

        public async Task CopyFile(string sourcePath, string destPath, bool copyMetadata = false)
        {
            EnsureAccess(sourcePath, FileAccess.Read);
            EnsureAccess(destPath, FileAccess.ReadWrite);

            using (var srcStream = GetFile(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                using (var destStream = GetFile(destPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read)) {
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
            EnsureAccess(sourcePath, FileAccess.ReadWrite);
            EnsureAccess(destPath, FileAccess.ReadWrite);

            PerformIO(p => File.Move(sourcePath, destPath), null);
        }

        public void MoveDirectory(string sourcePath, string destPath)
        {
            EnsureAccess(sourcePath, FileAccess.ReadWrite);
            EnsureAccess(destPath, FileAccess.ReadWrite);

            PerformIO(p => Directory.Move(sourcePath, destPath), null);
        }

        public void DeleteFile(string path)
        {
            EnsureAccess(path, FileAccess.ReadWrite);

            PerformIO(p => File.Delete(p), path);
        }

        public void DeleteDirectory(string path)
        {
            EnsureAccess(path, FileAccess.ReadWrite);

            PerformIO(p => Directory.Delete(p, true), path);
        }

        public FileInfo CreateFile(string path)
        {
            EnsureAccess(path, FileAccess.ReadWrite);

            return PerformIO(p => {
                File.Create(p).Dispose();
                return new FileInfo(p);
            }, path);
        }

        public DirectoryInfo CreateDirectory(string path)
        {
            EnsureAccess(path, FileAccess.ReadWrite);

            return PerformIO(p => Directory.CreateDirectory(p), path);
        }

        public bool FileExists(string path)
        {
            EnsureAccess(path, FileAccess.Read);

            return PerformIO(p => File.Exists(p), path);
        }

        public bool DirectoryExists(string path)
        {
            EnsureAccess(path, FileAccess.Read);

            return PerformIO(p => Directory.Exists(p), path);
        }

        public bool IsAccessAllowed(string path, FileAccess requestedAccess)
        {
            var allowedAccess = _accessControl.GetFileAccess(path);

            return (!requestedAccess.HasFlag(FileAccess.Read) || allowedAccess.HasFlag(FileAccess.Read))
                                         && (!requestedAccess.HasFlag(FileAccess.Write) || allowedAccess.HasFlag(FileAccess.Write));
        }



        private void EnsureAccess(string path, FileAccess fileAccess)
        {
            if (!IsAccessAllowed(path, fileAccess)) {

                if (fileAccess != FileAccess.Read && IsAccessAllowed(path, FileAccess.Read)) {
                    throw new ForbiddenPathException(path, ForbiddenPathException.PATH_IS_READ_ONLY);
                }

                throw new ForbiddenPathException(path);
            }
        }

        private void PerformIO(Action<string> action, string path)
        {
            PerformIO<object>(p => {
                action(p);
                return null;
            }, path);
        }

        private T PerformIO<T>(Func<string, T> func, string path)
        {
            try {
                return func(path);
            }
            catch (IOException e) {
                if (e.HResult == IOErrors.FileInUse) {
                    throw new LockedException(path);
                }

                throw;
            }
            catch (UnauthorizedAccessException) {
                throw new UnauthorizedArgumentException(path);
            }
        }
    }
}
