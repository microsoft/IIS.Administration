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
        private IFileProvider _fileProvider;

        public MoveOperation(Task task, IFileInfo source, string destination, string tempPath, IFileProvider fileProvider)
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
            _fileProvider = fileProvider;
        }

        public string Id { get; private set; }
        public Task Task { get; private set; }
        public FileType Type { get; private set; }
        public string TempPath { get; private set; }
        public DateTime Created { get; private set; }
        public string Destination { get; private set; }
        public IFileInfo Source { get; private set; }

        public long TotalSize {
            get {
                if (_totalSize != -1) {
                    return _totalSize;
                }

                if (Source.Type == FileType.File) {
                    _totalSize = Source.Exists ? Source.Size : 0;
                }
                else {
                    _totalSize = Source.Exists ? _fileProvider.GetFiles(Source.Path, "*", SearchOption.AllDirectories).Aggregate(0L, (prev, f) => prev + (f.Exists ? f.Size : 0)) : 0;
                }

                return _totalSize;
            }
        }

        public long CurrentSize {
            get {
                if (_currentSize != -1) {
                    return _currentSize;
                }

                if (Source.Type == FileType.File) {
                    var dest = string.IsNullOrEmpty(TempPath) ? _fileProvider.GetFile(Destination) : _fileProvider.GetFile(TempPath);
                    return dest.Exists ? dest.Size : 0;
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
