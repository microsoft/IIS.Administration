// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRequestTracing
{
    using AspNetCore.Hosting;
    using Core;
    using Files;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    class FrebXslFileProvider : IFileProvider
    {
        private IFileProvider _next;
        private FrebXslLocator _locator;

        public FrebXslFileProvider(IFileProvider next, IHostingEnvironment env, IApplicationHostConfigProvider configProvider)
        {
            _next = next;
            _locator = new FrebXslLocator(env, configProvider);
        }

        public IFileOptions Options {
            get {
                return _next.Options;
            }
        }

        public Task Copy(IFileInfo source, IFileInfo destination)
        {
            return _next.Copy(source, destination);
        }

        public IFileInfo CreateDirectory(IFileInfo directory)
        {
            return _next.CreateDirectory(directory);
        }

        public IFileInfo CreateFile(IFileInfo file)
        {
            return _next.CreateFile(file);
        }

        public void Delete(IFileInfo info)
        {
            _next.Delete(info);
        }

        public IEnumerable<IFileInfo> GetDirectories(IFileInfo info, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            return _next.GetDirectories(info, searchPattern, searchOption);
        }

        public IFileInfo GetDirectory(string path)
        {
            return _next.GetDirectory(path);
        }

        public IFileInfo GetFile(string path)
        {
            if (path.Equals("freb.xsl", StringComparison.OrdinalIgnoreCase)) {
                return new FrebXslFileInfo(_locator.Path == null ? null : new FileInfo(_locator.Path));
            }

            return _next.GetFile(path);
        }

        public IEnumerable<IFileInfo> GetFiles(IFileInfo info, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            return _next.GetFiles(info, searchPattern, searchOption);
        }

        public Stream GetFileStream(IFileInfo info, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            if (info.Path.Equals("freb.xsl", StringComparison.OrdinalIgnoreCase)) {
                if (fileAccess.HasFlag(FileAccess.Write)) {
                    throw new ForbiddenArgumentException("path");
                }

                if (_locator.Path == null) {
                    throw new NotFoundException("path");
                }
                
                return new FileStream(_locator.Path, FileMode.Open, FileAccess.Read, fileShare);
            }

            return _next.GetFileStream(info, fileMode, fileAccess, fileShare);
        }

        public bool IsAccessAllowed(IFileInfo fileInfo, FileAccess requestedAccess)
        {
            if (fileInfo.Path.Equals(FrebXslFileInfo.FILE_NAME, StringComparison.OrdinalIgnoreCase)) {
                return !requestedAccess.HasFlag(FileAccess.Write);
            }

            return _next.IsAccessAllowed(fileInfo, requestedAccess);
        }

        public bool IsAccessAllowed(string path, FileAccess requestedAccess)
        {
            if (path.Equals(FrebXslFileInfo.FILE_NAME, StringComparison.OrdinalIgnoreCase)) {
                return !requestedAccess.HasFlag(FileAccess.Write);
            }

            return _next.IsAccessAllowed(path, requestedAccess);
        }

        public void Move(IFileInfo source, IFileInfo destination)
        {
            _next.Move(source, destination);
        }

        public void SetFileTime(IFileInfo info, DateTime? lastAccessed, DateTime? lastModified, DateTime? created)
        {
            _next.SetFileTime(info, lastAccessed, lastModified, created);
        }
    }
}
