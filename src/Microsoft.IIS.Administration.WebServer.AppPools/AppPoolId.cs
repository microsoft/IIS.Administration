// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.AppPools
{
    public class AppPoolId
    {
        private const string PURPOSE = "WebServer.AppPools";
        
        public string Name { get; private set; }

        public string Uuid { get; private set; }

        private AppPoolId() { }

        public static AppPoolId CreateFromName(string name)
        {
            string uuid = Core.Utils.Uuid.Encode(name, PURPOSE);
            return new AppPoolId() {
                Name = name,
                Uuid = uuid
            };
        }

        public static AppPoolId CreateFromUuid(string uuid)
        {
            string name = Core.Utils.Uuid.Decode(uuid, PURPOSE);
            return new AppPoolId() {
                Uuid = uuid,
                Name = name
            };
        }
    }
}
