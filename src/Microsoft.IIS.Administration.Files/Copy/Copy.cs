// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using Core.Utils;
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Threading.Tasks;

    class Copy
    {
        public Copy(Task task, string source, string destination, string tempPath)
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
        public string Source { get; private set; }
        public string TempPath { get; private set; }
        public string Destination { get; private set; }
        public DateTime Created { get; private set; }
    }

    static class CopyHelper
    {
        public static object ToJsonModel(Copy copy)
        {
            var tempInfo = new FileInfo(copy.TempPath);
            var sourceInfo = new FileInfo(copy.Source);

            var obj = new {
                id = copy.Id,
                status = "running",
                current_size = tempInfo.Exists ? tempInfo.Length : 0,
                total_size = sourceInfo.Exists ? sourceInfo.Length : 0,
                created = copy.Created,
                file = FilesHelper.FileToJsonModelRef(new FileInfo(copy.Destination))
            };

            return Core.Environment.Hal.Apply(Defines.CopyResource.Guid, obj);
        }

        public static string GetLocation(string id)
        {
            return $"/{Defines.COPY_PATH}/{id}";
        }
    }
}
