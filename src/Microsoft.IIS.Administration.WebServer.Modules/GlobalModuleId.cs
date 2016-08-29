// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Modules
{
    using System;

    public class GlobalModuleId
    {
        private const string PURPOSE = "WebServer.GlobalModules";

        private const uint NAME_INDEX = 0;

        public string Name { get; private set; }
        public string Uuid { get; private set; }

        private GlobalModuleId() { }

        public static GlobalModuleId CreateFromUuid(string uuid)
        {
            if (string.IsNullOrEmpty(uuid)) {
                throw new ArgumentNullException("uuid");
            }

            string info = Core.Utils.Uuid.Decode(uuid, PURPOSE);

            string name = info;

            return new GlobalModuleId() {
                Name = name,
                Uuid = uuid
            };
        }

        public static GlobalModuleId CreateFromName(string name)
        {
            if (string.IsNullOrEmpty(name)) {
                throw new ArgumentNullException("name");
            }

            string uuid = Core.Utils.Uuid.Encode(name, PURPOSE);

            return new GlobalModuleId {
                Uuid = uuid,
                Name = name
            };
        }
    }
}
