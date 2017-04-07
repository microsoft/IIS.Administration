// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Certificates
{
    using System;

    public class CertificateId
    {
        private const string PURPOSE = "Certificates";
        private const char DELIMITER = '\n';

        private const uint THUMBPRINT_INDEX = 0;
        private const uint STORE_NAME_INDEX = 1;

        public string Thumbprint { get; private set; }
        public string StoreName { get; private set; }

        public string Uuid { get; private set; }

        public CertificateId(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) {
                throw new ArgumentNullException(uuid);
            }

            var info = Core.Utils.Uuid.Decode(uuid, PURPOSE).Split(DELIMITER);

            this.Thumbprint = info[THUMBPRINT_INDEX];
            this.StoreName =  info[STORE_NAME_INDEX];

            this.Uuid = uuid;
        }

        public CertificateId(string thumbprint, string storeName)
        {
            this.Thumbprint = thumbprint;
            this.StoreName = storeName;

            this.Uuid = Core.Utils.Uuid.Encode($"{ this.Thumbprint }{ DELIMITER }{ this.StoreName }", PURPOSE);
        }
    }
}
