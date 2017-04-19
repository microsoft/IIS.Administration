// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Certificates
{
    using Core;
    using System;
    using System.Linq;

    public class CertificateId
    {
        private const string PURPOSE = "Certificates";
        private const char DELIMITER = '\n';

        private const uint STORE_NAME_INDEX = 0;
        private const uint ID_INDEX = 1;

        public string StoreName { get; private set; }
        public string Id { get; private set; }

        public string Uuid { get; private set; }

        public CertificateId(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) {
                throw new ArgumentNullException(uuid);
            }

            string info = Core.Utils.Uuid.Decode(uuid, PURPOSE);

            int delimiter = info.IndexOf(DELIMITER);

            if (delimiter < 0) {
                throw new NotFoundException(null);
            }

            this.StoreName = info.Substring(0, delimiter);
            this.Id = info.Substring(delimiter + 1);

            this.Uuid = uuid;
        }

        public CertificateId(string id, string storeName)
        {
            this.StoreName = storeName;
            this.Id = id;

            this.Uuid = Core.Utils.Uuid.Encode($"{ this.StoreName }{ DELIMITER }{ this.Id }", PURPOSE);
        }
    }
}
