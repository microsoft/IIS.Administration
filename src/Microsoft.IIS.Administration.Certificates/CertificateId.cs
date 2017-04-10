// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Certificates
{
    using System;

    public class CertificateId
    {
        private const string PURPOSE = "Certificates";
        private const char DELIMITER = '\n';

        private const uint Id_INDEX = 0;
        private const uint STORE_NAME_INDEX = 1;

        public string Id { get; private set; }
        public string StoreName { get; private set; }

        public string Uuid { get; private set; }

        public CertificateId(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) {
                throw new ArgumentNullException(uuid);
            }

            var info = Core.Utils.Uuid.Decode(uuid, PURPOSE).Split(DELIMITER);

            this.Id = info[Id_INDEX];
            this.StoreName = info[STORE_NAME_INDEX];

            this.Uuid = uuid;
        }

        public CertificateId(string id, string storeName)
        {
            this.Id = id;
            this.StoreName = storeName;

            this.Uuid = Core.Utils.Uuid.Encode($"{ this.Id }{ DELIMITER }{ this.StoreName }", PURPOSE);
        }
    }
}
