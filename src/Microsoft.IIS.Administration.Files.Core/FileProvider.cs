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

        public Stream GetFileStream(IFileInfo file, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            this.EnsureAccess(file, fileAccess);

            return PerformIO(p => new FileStream(p, fileMode, fileAccess, fileShare), file.Path);
        }

        public IFileInfo GetFile(string path)
        {
            return PerformIO(p => {

                var info = new FileInfo(Interop.GetPath(path), _accessControl, _options);

                if (!IsAccessAllowed(info, FileAccess.Read) && (info.Parent == null || !IsAccessAllowed(info.Parent, FileAccess.Read))) {
                    throw new ForbiddenArgumentException(p);
                }

                return info;

            }, path);
        }

        public IFileInfo GetDirectory(string path)
        {
            return PerformIO(p => {

                var info = new DirectoryInfo(Interop.GetPath(path), _accessControl, _options);

                if (!IsAccessAllowed(info, FileAccess.Read) && (info.Parent == null || !IsAccessAllowed(info.Parent, FileAccess.Read))) {

                    throw new ForbiddenArgumentException(p);
                }

                return info;

            }, path);
        }

        public IEnumerable<IFileInfo> GetFiles(IFileInfo parent, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            this.EnsureAccess(parent, FileAccess.Read);

            return PerformIO(p => Directory.GetFiles(p ,searchPattern, searchOption), parent.Path).Select(f => new FileInfo(f, _accessControl, _options,true));
        }

        public IEnumerable<IFileInfo> GetDirectories(IFileInfo parent, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            //
            // Get roots
            if (parent == null) {
                var dirs = new List<IFileInfo>();

                foreach (var location in Options.Locations) {
                    if (IsAccessAllowed(location.Path, FileAccess.Read) &&
                            (string.IsNullOrEmpty(searchPattern) || PathUtil.GetName(location.Path).IndexOf(searchPattern, StringComparison.OrdinalIgnoreCase) != -1)) {

                        dirs.Add(GetDirectory(location.Path));
                    }
                }
                return dirs;
            }


            this.EnsureAccess(parent, FileAccess.Read);
            return PerformIO(p => Directory.GetDirectories(p, searchPattern, searchOption), parent.Path).Select(d => new DirectoryInfo(d, _accessControl, _options, true));
        }

        public async Task Copy(IFileInfo source, IFileInfo destination)
        {
            this.EnsureAccess(source, FileAccess.Read);
            this.EnsureAccess(destination, FileAccess.ReadWrite);

            if (!PerformIO(p => File.Exists(source.Path), source.Path)) {
                throw new FileNotFoundException(source.Path);
            }

            await CopyFileInternal(source.Path, destination.Path);
        }

        public void Move(IFileInfo source, IFileInfo destination)
        {
            this.EnsureAccess(source, FileAccess.ReadWrite);
            this.EnsureAccess(destination, FileAccess.ReadWrite);

            PerformIO(p => Directory.Move(source.Path, destination.Path), null);
        }

        public void Delete(IFileInfo info)
        {
            this.EnsureAccess(info, FileAccess.ReadWrite);

            PerformIO(p => {
                if (info.Type == FileType.Directory) {
                    // Delete files
                    foreach (var file in GetFiles(info, "*")) {
                        Delete(file);
                    }

                    // Recursively delete subdirectories
                    foreach (var dir in GetDirectories(info, "*")) {
                        Delete(dir);
                    }

                    if (Directory.GetFileSystemEntries(info.Path, "*", SearchOption.AllDirectories).Count() == 0) {
                        Directory.Delete(info.Path);
                    }
                }
                else {
                    File.Delete(p);
                }
            }, info.Path);
        }

        public IFileInfo CreateFile(IFileInfo file)
        {
            string directory = Path.GetDirectoryName(file.Path);

            if (string.IsNullOrEmpty(directory)) {
                throw new ArgumentException(nameof(file));
            }

            this.EnsureAccess(file, FileAccess.ReadWrite);
            this.EnsureAccess(directory, FileAccess.ReadWrite);

            return PerformIO(p => {
                File.Create(p).Dispose();
                return new FileInfo(p, _accessControl, _options);
            }, file.Path);
        }

        public IFileInfo CreateDirectory(IFileInfo directory)
        {
            string parent = Path.GetDirectoryName(directory.Path);

            if (string.IsNullOrEmpty(parent)) {
                throw new ArgumentException(nameof(directory));
            }

            this.EnsureAccess(directory, FileAccess.ReadWrite);
            this.EnsureAccess(parent, FileAccess.ReadWrite);

            return PerformIO(p => {
                Directory.CreateDirectory(p);
                return new DirectoryInfo(p, _accessControl, _options);
            }, directory.Path);
        }

        public bool IsAccessAllowed(string path, FileAccess requestedAccess)
        {
            return IsAccessAllowed(new FileInfo(path, _accessControl, _options), requestedAccess);
        }

        public bool IsAccessAllowed(IFileInfo fileInfo, FileAccess requestedAccess)
        {
            return (!requestedAccess.HasFlag(FileAccess.Read) || fileInfo.Claims.Contains("read", StringComparer.OrdinalIgnoreCase))
                                         && (!requestedAccess.HasFlag(FileAccess.Write) || fileInfo.Claims.Contains("write", StringComparer.OrdinalIgnoreCase));
        }

        public void SetFileTime(IFileInfo file, DateTime? lastAccessed, DateTime? lastModified, DateTime? created)
        {
            PerformIO(p => {

                if (lastAccessed != null) {
                    Directory.SetLastAccessTime(p, lastAccessed.Value);
                    file.LastAccessed = lastAccessed.Value;
                }

                if (lastModified != null) {
                    Directory.SetLastWriteTime(p, lastModified.Value);
                    file.LastModified = lastModified.Value;
                }

                if (created != null) {
                    Directory.SetCreationTime(p, created.Value);
                    file.Created = created.Value;
                }

            }, file.Path);
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
                if (e.HResult == HResults.FileInUse) {
                    throw new LockedException(path);
                }

                throw;
            }
            catch (UnauthorizedAccessException) {
                throw new ForbiddenArgumentException(path);
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

        public static void EnsureAccess(this IFileProvider provider, IFileInfo info, FileAccess fileAccess)
        {
            if (!provider.IsAccessAllowed(info, fileAccess)) {

                if (fileAccess != FileAccess.Read && provider.IsAccessAllowed(info, FileAccess.Read)) {
                    throw new ForbiddenArgumentException(info.Path, PATH_IS_READ_ONLY);
                }

                throw new ForbiddenArgumentException(info.Path);
            }
        }
    }
}
