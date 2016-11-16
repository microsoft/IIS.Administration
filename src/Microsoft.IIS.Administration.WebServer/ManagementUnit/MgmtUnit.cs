// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer
{
    using Web.Administration;

    class MgmtUnit : IManagementUnit {
        public ServerManager ServerManager { get; private set; }

        public MgmtUnit() {
            ServerManager = new ServerManager(ApplicationHostConfigPath);
        }

        public string ApplicationHostConfigPath {
            get {
                //
                // At the moment support global applicationHost.config
                return null;
            }
        }

        public bool Commit() {
            ServerManager.CommitChanges();
            return true;
        }

        public void Dispose() {
            if(ServerManager != null) {
                ServerManager.Dispose();
                ServerManager = null;
            }
        }
    }
}
