// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using System;
    using System.IO;

    public interface IFileSystemInfo
    {
        string Name { get; }
        string Path { get; }
        bool Exists { get; }
        FileAttributes Attributes { get; }
        DateTime LastAccess { get; }
        DateTime LastModified { get; }
        DateTime Creation { get; }
    }
}