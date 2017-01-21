// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using Core.Utils;
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Threading.Tasks;

    class MoveOperation
    {
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
    }
}
