// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer
{
    public class WebServerId {
        private const string PURPOSE = "IIS.WebServer";
        
        public string ApplicationHostConfigPath { get; private set; }

        public string Uuid { get; private set; }

        private WebServerId() { }

        public static WebServerId CreateFromPath(string applicationHostConfigPath) {
            string uuid = Core.Utils.Uuid.Encode(applicationHostConfigPath ?? string.Empty, PURPOSE);
            return new WebServerId() {
                ApplicationHostConfigPath = applicationHostConfigPath,
                Uuid = uuid
            };
        }

        public static WebServerId CreateFromUuid(string uuid) {
            string appHostConfigPath = Core.Utils.Uuid.Decode(uuid, PURPOSE);
            return new WebServerId() {
                Uuid = uuid,
                ApplicationHostConfigPath = string.IsNullOrEmpty(appHostConfigPath) ? null : appHostConfigPath
            };
        }
    }
}
