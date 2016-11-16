// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Transactions
{
    using Web.Administration;
    using System;

    class ManagementUnit : IManagementUnit
    {
        private string _appHostConfigPath;

        public Transaction Transaction{ get;}
        public bool CommitRequested { get; set; }

        public ManagementUnit(Transaction transaction)
        {
            this.Transaction = transaction;
            this._appHostConfigPath = WebServer.ManagementUnit.Current.ApplicationHostConfigPath;
            this.ServerManager = new ServerManager(_appHostConfigPath);
        }

        private ManagementUnit()
        {
            this._appHostConfigPath = WebServer.ManagementUnit.Current.ApplicationHostConfigPath;
            this.ServerManager = new ServerManager(_appHostConfigPath);
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
                return _appHostConfigPath;
            }
        }

        //public static ManagementUnit EmptyManagementUnit()
        //{
        //    return new ManagementUnit();
        //}
    }
}
