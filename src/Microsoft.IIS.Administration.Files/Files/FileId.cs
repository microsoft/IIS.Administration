// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using Core;
    using System;

    public class FileId
    {
        private const string PURPOSE = "Files";
        private const char DELIMITER = '\n';
        private const uint PHYSICAL_PATH_INDEX = 0;

        private FileId() { }

        public static FileId FromUuid(string uuid)
        {
            if (String.IsNullOrEmpty(uuid)) {
                throw new ArgumentNullException("uuid");
            }

            var fileId = new FileId();

            string[] info = Core.Utils.Uuid.Decode(uuid, PURPOSE).Split(DELIMITER);

            var physicalPath = info[PHYSICAL_PATH_INDEX];

            if (!PathUtil.IsFullPath(physicalPath)) {
                throw new NotFoundException("file.id");
            }

            return new FileId() {
                PhysicalPath = physicalPath,
                Uuid = uuid
            };
        }

        public static FileId FromPhysicalPath(string physicalPath)
        {
            if (physicalPath == null) {
                throw new ArgumentNullException("path");
            }
            if (!PathUtil.IsFullPath(physicalPath)) {
                throw new ArgumentException("Path must be full path.", nameof(physicalPath));
            };

            return new FileId() {
                Uuid = Core.Utils.Uuid.Encode($"{physicalPath}", PURPOSE),
                PhysicalPath = physicalPath
            };
        }
        
        public string PhysicalPath { get; private set; }
        public string Uuid { get; private set; }
    }
}
