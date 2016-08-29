// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Transactions
{
    using Microsoft.Web.Administration;
    using Microsoft.IIS.Administration.Core;
    using System;

    class ManagementUnit : IManagementUnit
    {
        public Transaction Transaction{ get;}
        public bool CommitRequested { get; set; }

        public ManagementUnit(Transaction transaction)
        {
            this.Transaction = transaction;
            this.ServerManager = new ServerManager(WebServer.ManagementUnit.Current.ApplicationHostConfigPath);
        }

        public bool Commit()
        {
            CommitRequested = true;
            return false;
        }

        public void Dispose()
        {
            if (ServerManager != null) {
                ServerManager.Dispose();
                ServerManager = null;
            }
        }

        public ServerManager ServerManager { get; private set; }

        public string ApplicationHostConfigPath {
            get {
                throw new NotImplementedException();
            }
        }
    }
}
