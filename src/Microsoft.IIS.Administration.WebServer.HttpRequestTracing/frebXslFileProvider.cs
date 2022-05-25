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

        public FrebXslFileProvider(IFileProvider next, IWebHostEnvironment env, IApplicationHostConfigProvider configProvider)
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
            return path.Equals("freb.xsl", StringComparison.OrdinalIgnoreCase)
                ? new FrebXslFileInfo(_locator.Path == null ? null : new FileInfo(_locator.Path))
                : _next.GetFile(path);
        }

        public IEnumerable<IFileInfo> GetFiles(IFileInfo info, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            return _next.GetFiles(info, searchPattern, searchOption);
        }

        public Stream GetFileStream(IFileInfo info, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            return info.Path.Equals("freb.xsl", StringComparison.OrdinalIgnoreCase)
                ? fileAccess.HasFlag(FileAccess.Write)
                    ? throw new ForbiddenArgumentException("path")
                    : _locator.Path == null
                    ? throw new NotFoundException("path")
                    : (Stream)new FileStream(_locator.Path, FileMode.Open, FileAccess.Read, fileShare)
                : _next.GetFileStream(info, fileMode, fileAccess, fileShare);
        }

        public bool IsAccessAllowed(IFileInfo fileInfo, FileAccess requestedAccess)
        {
            return fileInfo.Path.Equals(FrebXslFileInfo.FILE_NAME, StringComparison.OrdinalIgnoreCase)
                ? !requestedAccess.HasFlag(FileAccess.Write)
                : _next.IsAccessAllowed(fileInfo, requestedAccess);
        }

        public bool IsAccessAllowed(string path, FileAccess requestedAccess)
        {
            return path.Equals(FrebXslFileInfo.FILE_NAME, StringComparison.OrdinalIgnoreCase)
                ? !requestedAccess.HasFlag(FileAccess.Write)
                : _next.IsAccessAllowed(path, requestedAccess);
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
