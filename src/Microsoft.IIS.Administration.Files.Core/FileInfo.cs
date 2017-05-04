// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    class FileInfo : IFileInfo
    {
        private SymLink _symlink;
        private IFileInfo _parent;
        private bool _resolvedParent;
        private IFileOptions _options;
        private System.IO.FileInfo _info;
        private IEnumerable<string> _claims;
        private IAccessControl _accessControl;

        private bool? _exists;
        private DateTime? _created;
        private DateTime? _lastModified;
        private DateTime? _lastAccessed;

        public FileInfo(string path, IAccessControl accessControl, IFileOptions options)
        {
            _info = new System.IO.FileInfo(path);
            _accessControl = accessControl;
            _options = options;

            if (!options.SkipResolvingSymbolicLinks) {
                _symlink = new SymLink(path);
            }
        }

        public FileInfo(string path, IAccessControl accessControl, IFileOptions options, bool exists) : this(path, accessControl, options)
        {
            _exists = exists;
        }

        public IEnumerable<string> Claims {
            get {
                if (_claims == null) {
                    try {
                        _claims = _accessControl.GetClaims(Target);
                    }
                    //
                    // Ignore
                    catch (UnauthorizedAccessException) {
                    }
                    catch (IOException) {
                    }

                    _claims = _claims ?? Enumerable.Empty<string>();
                }
                return _claims;
            }
        }

        public FileAttributes Attributes {
            get {
                return _info.Attributes;
            }
        }

        public bool Exists {
            get {
                return _exists ?? _info.Exists;
            }
        }

        public DateTime Created {
            get {
                return _created ?? _info.CreationTime;
            }
            set {
                _created = value;
            }
        }

        public DateTime LastAccessed {
            get {
                return _lastAccessed ?? _info.LastAccessTime;
            }
            set {
                _lastAccessed = value;
            }
        }

        public DateTime LastModified {
            get {
                return _lastModified ?? _info.LastWriteTime;
            }
            set {
                _lastModified = value;
            }
        }

        public string Name {
            get {
                return _info.Name;
            }
        }

        public IFileInfo Parent {
            get {
                if (!_resolvedParent) {
                    _parent = _info.Directory == null ? null : new DirectoryInfo(_info.Directory.FullName, _accessControl, _options);
                    _resolvedParent = true;
                }
                return _parent;
            }
        }

        public string Path {
            get {
                return _info.FullName;
            }
        }
        public string Target {
            get {
                return _symlink?.Target ?? Path;
            }
        }

        public long Size {
            get {
                try {
                    return _info.Length;
                }
                catch (FileNotFoundException) {
                    return 0;
                }
            }
        }

        public FileType Type {
            get {
                return FileType.File;
            }
        }
    }
}
