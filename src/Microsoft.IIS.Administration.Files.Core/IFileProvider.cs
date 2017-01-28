// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;

    public interface IFileProvider
    {
        Stream GetFileStream(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare);

        IFileInfo GetFile(string path);

        IDirectoryInfo GetDirectory(string path);

        FileVersionInfo GetFileVersion(string path);

        IEnumerable<IFileInfo> GetFiles(string path, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly);

        IEnumerable<IDirectoryInfo> GetDirectories(string path, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly);

        Task Copy(string sourcePath, string destPath);

        void Move(string sourcePath, string destPath);

        void Delete(string path);

        IFileInfo CreateFile(string path);

        IDirectoryInfo CreateDirectory(string path);

        bool FileExists(string path);

        bool DirectoryExists(string path);

        IEnumerable<string> GetClaims(string path);

        bool IsAccessAllowed(string path, FileAccess requestedAccess);

        void SetFileTime(string path, DateTime? lastAccess, DateTime? lastModified, DateTime? creation);
    }
}
