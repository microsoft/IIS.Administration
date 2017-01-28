// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using System;
    using System.IO;

    public class ETag
    {
        private ETag() { }

        public string Value { get; private set; }

        public static ETag Create(IFileInfo info)
        {
            if (!info.Exists) {
                throw new FileNotFoundException(info.Name);
            }

            DateTimeOffset last = info.LastModified.ToUniversalTime();
            var lastModified = new DateTimeOffset(last.Year, last.Month, last.Day, last.Hour, last.Minute, last.Second, last.Offset).ToUniversalTime();

            long etagHash = lastModified.ToFileTime() ^ info.Size;

            return new ETag()
            {
                Value = Convert.ToString(etagHash, 16)
            };
        }
    }
}
