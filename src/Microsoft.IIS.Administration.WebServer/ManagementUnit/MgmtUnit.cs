// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer
{
    using Core;
    using Core.Http;
    using Web.Administration;

    class MgmtUnit : IManagementUnit {
        public ServerManager ServerManager { get; private set; }

        public MgmtUnit()
        {
            this.ServerManager = new ServerManager(ApplicationHostConfigPath);
        }

        public bool CommitRequested { get; private set; }

        public string ApplicationHostConfigPath {
            get {
                //
                // At the moment support global applicationHost.config
                return null;
            }
        }

        public bool Commit() {

            var activeTransaction = Store.Transaction;

            if (activeTransaction != null) {
                var requestTransactionId = HttpHelper.Current.GetTransactionId();

                if (requestTransactionId == null || !requestTransactionId.Equals(activeTransaction.Id)) {
                    throw new NotFoundException("transaction");
                }
            }

            if (activeTransaction == null) {
                ServerManager.CommitChanges();
                return true;
            }
            else {
                CommitRequested = true;
                return false;
            }
        }

        public void Dispose() {
            if(ServerManager != null) {
                ServerManager.Dispose();
                ServerManager = null;
            }
        }
    }
}
