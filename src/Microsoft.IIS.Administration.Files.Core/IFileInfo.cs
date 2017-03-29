// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public interface IFileInfo
    {
        string Name { get; }
        string Path { get; }
        string Target { get; }
        bool Exists { get; }
        long Size { get; }
        FileType Type { get; }
        IFileInfo Parent { get; }
        IEnumerable<string> Claims { get; }
        FileAttributes Attributes { get; }
        DateTime LastAccessed { get; set; }
        DateTime LastModified { get; set; }
        DateTime Created { get; set; }
    }
}