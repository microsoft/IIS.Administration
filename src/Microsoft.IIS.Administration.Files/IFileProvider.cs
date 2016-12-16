// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;

    public interface IFileProvider
    {
        string GetName(string path);

        string GetParentPath(string path);

        Stream GetFile(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare);

        FileInfo GetFileInfo(string path);

        FileVersionInfo GetFileVersionInfo(string path);

        DirectoryInfo GetDirectoryInfo(string path);

        FileSystemInfo GetFileSystemInfo(string path);

        IEnumerable<FileInfo> GetFiles(string path, string searchPattern);

        IEnumerable<DirectoryInfo> GetDirectories(string path, string searchPattern);

        IEnumerable<FileSystemInfo> GetChildren(string path, string searchPattern);

        Task CopyFile(string sourcePath, string destPath, bool copyMetadata = false);

        void MoveFile(string sourcePath, string destPath);

        void MoveDirectory(string sourcePath, string destPath);

        void DeleteFile(string path);

        void DeleteDirectory(string path);

        FileInfo CreateFile(string path);

        DirectoryInfo CreateDirectory(string path);

        bool FileExists(string path);

        bool DirectoryExists(string path);

        bool IsAccessAllowed(string path, FileAccess requestedAccess);
    }
}
