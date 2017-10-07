// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer
{
    public class WebServerId {
        private const string PURPOSE = "IIS.WebServer";
        
        public string ApplicationHostConfigPath { get; private set; }

        public string Uuid { get; private set; }

        private WebServerId() { }

        public static WebServerId Create() {
            string uuid = Core.Utils.Uuid.Encode(string.Empty, PURPOSE);
            return new WebServerId() {
                ApplicationHostConfigPath = null,
                Uuid = uuid
            };
        }
    }
}
