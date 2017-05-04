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
        private long _currentSize = -1;
        private IFileProvider _fileProvider;

        public MoveOperation(IFileInfo source, IFileInfo destination, string tempPath, IFileProvider fileProvider)
        {
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create()) {
                rng.GetBytes(bytes);
                this.Id = Base64.Encode(bytes);
            }
            
            this.Source = source;
            this.Destination = destination;
            this.Created = DateTime.UtcNow;
            this.TempPath = tempPath;
            _fileProvider = fileProvider;
            Initialize();
        }

        public string Id { get; private set; }
        public Task Task { get; set; }
        public FileType Type { get; private set; }
        public string TempPath { get; private set; }
        public DateTime Created { get; private set; }
        public IFileInfo Destination { get; private set; }
        public IFileInfo Source { get; private set; }
        public long TotalSize { get; private set; }

        public long CurrentSize {
            get {
                if (_currentSize != -1) {
                    return _currentSize;
                }

                if (Source.Type == FileType.File) {
                    // refresh
                    var dest = string.IsNullOrEmpty(TempPath) ? _fileProvider.GetFile(Destination.Path) : _fileProvider.GetFile(TempPath);
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

        private void Initialize()
        {
            if (Source.Type == FileType.File) {
                TotalSize = Source.Exists ? Source.Size : 0;
            }
            else {
                try {
                    TotalSize = Source.Exists ? _fileProvider.GetFiles(Source, "*", SearchOption.AllDirectories).Aggregate(0L, (prev, f) => prev + f.Size) : 0;
                }
                catch (DirectoryNotFoundException) {
                    TotalSize = 0;
                }
            }
        }
    }
}
