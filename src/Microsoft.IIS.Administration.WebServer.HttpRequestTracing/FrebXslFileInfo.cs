// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRequestTracing
{
    using Files;
    using System;
    using System.Collections.Generic;
    using System.IO;

    class FrebXslFileInfo : IFileInfo
    {
        public const string FILE_NAME = "freb.xsl";
        private static readonly IEnumerable<string> CLAIMS = new string[] { "Read" };
        private FileInfo _freb;

        public FrebXslFileInfo(FileInfo freb)
        {
            _freb = freb;
        }

        public FileAttributes Attributes {
            get {
                return _freb?.Attributes ?? 0;
            }
        }

        public IEnumerable<string> Claims {
            get {
                return CLAIMS;
            }
        }

        public DateTime Created {
            get {
                return _freb?.CreationTimeUtc ?? DateTime.UtcNow;
            }

            set {
            }
        }

        public bool Exists {
            get {
                return _freb?.Exists ?? false;
            }
        }

        public DateTime LastAccessed {
            get {
                return _freb?.LastAccessTimeUtc ?? DateTime.UtcNow;
            }

            set {
            }
        }

        public DateTime LastModified {
            get {
                return _freb?.LastWriteTimeUtc ?? DateTime.UtcNow;
            }

            set {
            }
        }

        public string Name {
            get {
                return "freb.xsl";
            }
        }

        public IFileInfo Parent {
            get {
                return null;
            }
        }

        public string Path {
            get {
                return "freb.xsl";
            }
        }

        public long Size {
            get {
                return _freb?.Length ?? 0;
            }
        }

        public string Target {
            get {
                return Name;
            }
        }

        public FileType Type {
            get {
                return FileType.File;
            }
        }
    }
}
