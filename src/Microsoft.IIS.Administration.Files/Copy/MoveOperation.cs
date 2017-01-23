// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using Core.Utils;
    using System;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Threading.Tasks;

    class MoveOperation
    {
        private long _totalSize = -1;
        private long _currentSize = -1;

        public MoveOperation(Task task, FileSystemInfo source, string destination, string tempPath)
        {
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create()) {
                rng.GetBytes(bytes);
                this.Id = Base64.Encode(bytes);
            }
            
            this.Task = task;
            this.Source = source;
            this.Destination = destination;
            this.Created = DateTime.UtcNow;
            this.TempPath = tempPath;
        }

        public string Id { get; private set; }
        public Task Task { get; private set; }
        public FileType Type { get; private set; }
        public string TempPath { get; private set; }
        public DateTime Created { get; private set; }
        public string Destination { get; private set; }
        public FileSystemInfo Source { get; private set; }

        public long TotalSize {
            get {
                if (_totalSize != -1) {
                    return _totalSize;
                }

                if (Source is FileInfo) {
                    _totalSize = Source.Exists ? ((FileInfo)Source).Length : 0;
                }
                else {
                    _totalSize = Source.Exists ? ((DirectoryInfo)Source).EnumerateFiles("*", SearchOption.AllDirectories).Aggregate(0L, (prev, f) => prev + f.Length) : 0;
                }

                return _totalSize;
            }
        }

        public long CurrentSize {
            get {
                if (_currentSize != -1) {
                    return _currentSize;
                }

                if (Source is FileInfo) {
                    var dest = string.IsNullOrEmpty(TempPath) ? new FileInfo(Destination) : new FileInfo(TempPath);
                    return dest.Exists ? dest.Length : 0;
                }
                else {
                    return 0;
                }
            }
            set {
                _currentSize = value;
            }
        }
    }
}
