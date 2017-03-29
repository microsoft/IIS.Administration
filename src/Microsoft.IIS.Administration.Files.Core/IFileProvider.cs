// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    public interface IFileProvider
    {
        Stream GetFileStream(IFileInfo info, FileMode fileMode, FileAccess fileAccess, FileShare fileShare);

        IFileInfo GetFile(string path);

        IFileInfo GetDirectory(string path);

        IEnumerable<IFileInfo> GetFiles(IFileInfo info, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly);

        IEnumerable<IFileInfo> GetDirectories(IFileInfo info, string searchPattern, SearchOption searchOption = SearchOption.TopDirectoryOnly);

        Task Copy(IFileInfo source, IFileInfo destination);

        void Move(IFileInfo source, IFileInfo destination);

        void Delete(IFileInfo info);

        IFileInfo CreateFile(IFileInfo file);

        IFileInfo CreateDirectory(IFileInfo directory);

        bool IsAccessAllowed(string path, FileAccess requestedAccess);

        bool IsAccessAllowed(IFileInfo fileInfo, FileAccess requestedAccess);

        void SetFileTime(IFileInfo info, DateTime? lastAccessed, DateTime? lastModified, DateTime? created);

        IFileOptions Options { get; }
    }
}
