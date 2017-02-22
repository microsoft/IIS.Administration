// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using Core;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    public class FileProvider : IFileProvider
    {
        private IAccessControl _accessControl;
        private IFileOptions _options;

        public FileProvider(IAccessControl accessControl, IFileOptions options)
        {
            if (accessControl == null) {
                throw new ArgumentNullException(nameof(accessControl));
            }
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }

            _options = options;
            _accessControl = accessControl;
        }

        public Stream GetFileStream(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            this.EnsureAccess(path, fileAccess);

            return PerformIO(p => new FileStream(p, fileMode, fileAccess, fileShare), path);
        }

        public IFileInfo GetFile(string path)
        {
            return PerformIO(p => {

                var info = new FileInfo(path);

                if (!IsAccessAllowed(path, FileAccess.Read) && (info.Parent == null || !IsAccessAllowed(info.Parent.Path, FileAccess.Read))) {
                    throw new ForbiddenArgumentException(p);
                }

                return info;

            }, path);
        }

        public IFileInfo GetDirectory(string path)
        {
            return PerformIO(p => {

                var info = new DirectoryInfo(path);

                if (!IsAccessAllowed(path, FileAccess.Read) && (info.Parent == null || !IsAccessAllowed(info.Parent.Path, FileAccess.Read))) {

                    throw new ForbiddenArgumentException(p);
                }

                return info;

            }, path);
        }

        public IEnumerable<IFileInfo> GetFiles(string path, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            this.EnsureAccess(path, FileAccess.Read);

            return PerformIO(p => Directory.GetFiles(p ,searchPattern, searchOption), path).Select(f => new FileInfo(f));
        }

        public IEnumerable<IFileInfo> GetDirectories(string path, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            this.EnsureAccess(path, FileAccess.Read);

            return PerformIO(p => Directory.GetDirectories(p, searchPattern, searchOption), path).Select(d => new DirectoryInfo(d));
        }

        public async Task Copy(string sourcePath, string destPath)
        {
            this.EnsureAccess(sourcePath, FileAccess.Read);
            this.EnsureAccess(destPath, FileAccess.ReadWrite);

            if (!PerformIO(p => File.Exists(sourcePath), sourcePath)) {
                throw new FileNotFoundException(sourcePath);
            }

            await CopyFileInternal(sourcePath, destPath);
        }

        public void Move(string sourcePath, string destPath)
        {
            this.EnsureAccess(sourcePath, FileAccess.ReadWrite);
            this.EnsureAccess(destPath, FileAccess.ReadWrite);

            PerformIO(p => Directory.Move(sourcePath, destPath), null);
        }

        public void Delete(string path)
        {
            this.EnsureAccess(path, FileAccess.ReadWrite);

            PerformIO(p => {
                if (File.GetAttributes(p).HasFlag(FileAttributes.Directory)) {
                    Directory.Delete(p, true);
                }
                else {
                    File.Delete(p);
                }
            }, path);
        }

        public IFileInfo CreateFile(string path)
        {
            this.EnsureAccess(path, FileAccess.ReadWrite);

            return PerformIO(p => {
                File.Create(p).Dispose();
                return new FileInfo(p);
            }, path);
        }

        public IFileInfo CreateDirectory(string path)
        {
            this.EnsureAccess(path, FileAccess.ReadWrite);

            return PerformIO(p => {
                Directory.CreateDirectory(p);
                return new DirectoryInfo(p);
            }, path);
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

        public IEnumerable<string> GetClaims(string path)
        {
            return _accessControl.GetClaims(path);
        }

        public bool IsAccessAllowed(string path, FileAccess requestedAccess)
        {
            var claims = _accessControl.GetClaims(path);

            return (!requestedAccess.HasFlag(FileAccess.Read) || claims.Contains("read", StringComparer.OrdinalIgnoreCase))
                                         && (!requestedAccess.HasFlag(FileAccess.Write) || claims.Contains("write", StringComparer.OrdinalIgnoreCase));
        }

        public void SetFileTime(string path, DateTime? lastAccessed, DateTime? lastModified, DateTime? created)
        {
            PerformIO(p => {

                if (lastAccessed != null) {
                    Directory.SetLastAccessTime(p, lastAccessed.Value);
                }

                if (lastModified != null) {
                    Directory.SetLastWriteTime(p, lastModified.Value);
                }

                if (created != null) {
                    Directory.SetCreationTime(p, created.Value);
                }

            }, path);
        }

        public IFileOptions Options {
            get {
                return _options;
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

        private async Task CopyFileInternal(string sourcePath, string destPath)
        {
            using (var srcStream = PerformIO(p => new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.Read), sourcePath)) {
                using (var destStream = PerformIO(p => new FileStream(p, FileMode.Create, FileAccess.Write, FileShare.Read), destPath)) {
                    await srcStream.CopyToAsync(destStream);
                }
            }

            var sourceFileInfo = new System.IO.FileInfo(sourcePath);
            var destFileInfo = new System.IO.FileInfo(destPath);

            destFileInfo.LastAccessTimeUtc = sourceFileInfo.LastAccessTimeUtc;
            destFileInfo.LastWriteTimeUtc = sourceFileInfo.LastWriteTimeUtc;
            destFileInfo.CreationTimeUtc = sourceFileInfo.CreationTimeUtc;
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
