// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Certificates
{
    using System;

    public class StoreId
    {
        private const string PURPOSE = "Certificates.Store";
        private const char DELIMITER = '\n';

        private const uint NAME_INDEX = 0;

        public string Name { get; private set; }

        public string Uuid { get; private set; }

        private StoreId() { }

        public static StoreId FromUuid(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) {
                throw new ArgumentNullException(uuid);
            }

            StoreId id = new StoreId();

            var info = Core.Utils.Uuid.Decode(uuid, PURPOSE).Split(DELIMITER);

            id.Name = info[NAME_INDEX];

            id.Uuid = uuid;

            return id;
        }

        public static StoreId FromName(string name)
        {
            StoreId id = new StoreId();

            id.Name = name;

            id.Uuid = Core.Utils.Uuid.Encode($"{ id.Name }", PURPOSE);

            return id;
        }
    }
}
