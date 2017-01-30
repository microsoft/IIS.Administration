// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using System;
    using System.IO;

    class DirectoryInfo : IFileInfo
    {
        private System.IO.DirectoryInfo _info;
        private IFileInfo _parent;

        public DirectoryInfo(string path)
        {
            _info = new System.IO.DirectoryInfo(path);
            _parent = _info.Parent == null ? null : new DirectoryInfo(_info.Parent.FullName);
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
                return 0;
            }
        }

        public FileType Type {
            get {
                return FileType.Directory;
            }
        }
    }
}
