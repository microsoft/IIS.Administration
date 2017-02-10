// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using Core.Utils;
    using System.Security.Cryptography;

    class Download : IDownload
    {
        internal Download()
        {
            var bytes = new byte[32];

            using (var rng = RandomNumberGenerator.Create()) {
                rng.GetBytes(bytes);
                this.Id = Base64.Encode(bytes);
            }
        }

        public string Id { get; private set; }

        public string PhysicalPath { get; set; }

        public string Href {
            get {
                return $"/{Defines.DOWNLOAD_PATH}/{Id}";
            }
        }
    }
}
