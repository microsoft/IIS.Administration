// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using Core;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
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
            return PerformIO(p => new FileInfo(path).Name , path);
        }

        public string GetParentPath(string path)
        {
            return PerformIO(p => new FileInfo(path).Directory?.FullName, path);
        }

        public Stream GetFile(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            this.EnsureAccess(path, fileAccess);

            return PerformIO(p => new FileStream(p, fileMode, fileAccess, fileShare), path);
        }

        public FileInfo GetFileInfo(string path)
        {
            this.EnsureAccess(path, FileAccess.Read);

            return PerformIO(p => new FileInfo(p), path);
        }

        public FileVersionInfo GetFileVersionInfo(string path)
        {
            this.EnsureAccess(path, FileAccess.Read);

            return PerformIO(p => FileVersionInfo.GetVersionInfo(p), path);
        }

        public DirectoryInfo GetDirectoryInfo(string path)
        {
            this.EnsureAccess(path, FileAccess.Read);

            return PerformIO(p => new DirectoryInfo(p), path);
        }

        public FileSystemInfo GetFileSystemInfo(string path)
        {
            this.EnsureAccess(path, FileAccess.Read);

            return PerformIO(p => new FileInfo(p), path);
        }

        public IEnumerable<FileInfo> GetFiles(string path, string searchPattern)
        {
            this.EnsureAccess(path, FileAccess.Read);

            return PerformIO(p => {
                return new DirectoryInfo(p).GetFiles(searchPattern);
            }, path);
        }

        public IEnumerable<DirectoryInfo> GetDirectories(string path, string searchPattern)
        {
            this.EnsureAccess(path, FileAccess.Read);

            return PerformIO(p => {
                return new DirectoryInfo(p).GetDirectories(searchPattern);
            }, path);
        }

        public IEnumerable<FileSystemInfo> GetChildren(string path, string searchPattern)
        {
            this.EnsureAccess(path, FileAccess.Read);

            return PerformIO(p => {
                return new DirectoryInfo(p).GetFileSystemInfos(searchPattern);
            }, path);
        }

        public async Task CopyFile(string sourcePath, string destPath, bool copyMetadata = false)
        {
            this.EnsureAccess(sourcePath, FileAccess.Read);
            this.EnsureAccess(destPath, FileAccess.ReadWrite);

            await PerformIO(async p => {

                using (var srcStream = GetFile(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    using (var destStream = GetFile(destPath, FileMode.Create, FileAccess.Write, FileShare.Read)) {
                        await srcStream.CopyToAsync(destStream);
                    }
                }

                if (copyMetadata) {
                    var sourceFileInfo = new FileInfo(sourcePath);
                    var destFileInfo = new FileInfo(destPath);

                    destFileInfo.CreationTime = sourceFileInfo.CreationTime;
                    destFileInfo.CreationTimeUtc = sourceFileInfo.CreationTimeUtc;
                }

            }, sourcePath);
        }

        public void MoveFile(string sourcePath, string destPath)
        {
            this.EnsureAccess(sourcePath, FileAccess.ReadWrite);
            this.EnsureAccess(destPath, FileAccess.ReadWrite);

            PerformIO(p => File.Move(sourcePath, destPath), null);
        }

        public void MoveDirectory(string sourcePath, string destPath)
        {
            this.EnsureAccess(sourcePath, FileAccess.ReadWrite);
            this.EnsureAccess(destPath, FileAccess.ReadWrite);

            PerformIO(p => Directory.Move(sourcePath, destPath), null);
        }

        public void DeleteFile(string path)
        {
            this.EnsureAccess(path, FileAccess.ReadWrite);

            PerformIO(p => File.Delete(p), path);
        }

        public void DeleteDirectory(string path)
        {
            this.EnsureAccess(path, FileAccess.ReadWrite);

            PerformIO(p => Directory.Delete(p, true), path);
        }

        public FileInfo CreateFile(string path)
        {
            this.EnsureAccess(path, FileAccess.ReadWrite);

            return PerformIO(p => {
                File.Create(p).Dispose();
                return new FileInfo(p);
            }, path);
        }

        public DirectoryInfo CreateDirectory(string path)
        {
            this.EnsureAccess(path, FileAccess.ReadWrite);

            return PerformIO(p => Directory.CreateDirectory(p), path);
        }

        public bool FileExists(string path)
        {
            this.EnsureAccess(path, FileAccess.Read);

            return PerformIO(p => File.Exists(p), path);
        }

        public bool DirectoryExists(string path)
        {
            this.EnsureAccess(path, FileAccess.Read);

            return PerformIO(p => Directory.Exists(p), path);
        }

        public bool IsAccessAllowed(string path, FileAccess requestedAccess)
        {
            var claims = _accessControl.GetClaims(path);

            return (!requestedAccess.HasFlag(FileAccess.Read) || claims.Contains("read", StringComparer.OrdinalIgnoreCase))
                                         && (!requestedAccess.HasFlag(FileAccess.Write) || claims.Contains("write", StringComparer.OrdinalIgnoreCase));
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

    public static class FileProviderExtensions
    {
        private const string PATH_IS_READ_ONLY = "Read-Only";

        public static void EnsureAccess(this IFileProvider provider, string path, FileAccess fileAccess)
        {
            if (!provider.IsAccessAllowed(path, fileAccess)) {

                if (fileAccess != FileAccess.Read && provider.IsAccessAllowed(path, FileAccess.Read)) {
                    throw new ForbiddenArgumentException(path, PATH_IS_READ_ONLY);
                }

                throw new ForbiddenArgumentException(path);
            }
        }
    }
}
