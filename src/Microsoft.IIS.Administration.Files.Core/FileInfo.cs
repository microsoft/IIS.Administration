// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using System;
    using System.IO;

    class FileInfo : IFileInfo
    {
        private System.IO.FileInfo _info;
        private IFileInfo _parent;

        public FileInfo(string path)
        {
            _info = new System.IO.FileInfo(path);
            _parent = _info.Directory == null ? null : new DirectoryInfo(_info.Directory.FullName);
        }

        public FileAttributes Attributes {
            get {
                return _info.Attributes;
            }
        }

        public DateTime Created {
            get {
                return _info.CreationTime;
            }
        }

        public bool Exists {
            get {
                return _info.Exists;
            }
        }

        public DateTime LastAccessed {
            get {
                return _info.LastAccessTime;
            }
        }

        public DateTime LastModified {
            get {
                return _info.LastWriteTime;
            }
        }

        public string Name {
            get {
                return _info.Name;
            }
        }

        public IFileInfo Parent {
            get {
                return _parent;
            }
        }

        public string Path {
            get {
                return _info.FullName;
            }
        }

        public long Size {
            get {
                return _info.Length;
            }
        }

        public FileType Type {
            get {
                return FileType.File;
            }
        }
    }
}
