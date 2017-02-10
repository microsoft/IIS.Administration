// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer
{
    using AspNetCore.Http;
    using System;
    using System.Threading;

    static class Store
    {
        private const int TRANSACTION_IDLE_TIMEOUT = 60000;

        private static object _lock = new object();
        private static Timer _timer;

        public static Transaction Transaction { get; private set; }

        public static MgmtUnit ManagementUnit { get; set; }

        public static Transaction BeginTransaction(IApplicationHostConfigProvider provider)
        {
            lock (_lock) {
                if (Transaction == null) {
                    var transaction = new Transaction();
                    transaction.ExpiresOn = transaction.CreatedOn.AddMilliseconds(TRANSACTION_IDLE_TIMEOUT);

                    _timer = new Timer(TimeoutCallback, null, TRANSACTION_IDLE_TIMEOUT, Timeout.Infinite);

                    
                    ManagementUnit = new MgmtUnit(provider);
                    Transaction = transaction;
                }
            }

            return Transaction;
        }

        public static Transaction KeepAliveTransaction()
        {
            lock (_lock) {
                if (Transaction != null) {
                    Transaction.ExpiresOn = DateTime.UtcNow.AddMilliseconds(TRANSACTION_IDLE_TIMEOUT);
                    _timer.Change(TRANSACTION_IDLE_TIMEOUT, Timeout.Infinite);
                }
            }

            return Transaction;
        }

        public static void CommitTransaction()
        {
            lock (_lock) {
                if (Transaction != null) {
                    _timer.Dispose();
                    _timer = null;

                    if (ManagementUnit.CommitRequested) {

                        try {
                            ManagementUnit.ServerManager.CommitChanges();
                        }
                        catch {
                            AbortTransaction();
                            throw;
                        }
                    }

                    Transaction = null;

                    ManagementUnit.Dispose();
                    ManagementUnit = null;
                }
            }
        }

        public static void AbortTransaction()
        {
            lock (_lock) {
                if (_timer != null) {
                    _timer.Dispose();
                    _timer = null;
                }

                Transaction = null;

                if (ManagementUnit != null) {
                    ManagementUnit.Dispose();
                    ManagementUnit = null;
                }
            }
        }

        public static string GetTransactionId(this HttpContext context)
        {
            string transactioId = context.Request.Headers[Defines.TRANSACTION_HEADER];
            return string.IsNullOrEmpty(transactioId) ? null : transactioId;
        }

        private static void TimeoutCallback(object stateInfo)
        {
            if (DateTime.UtcNow > Transaction.ExpiresOn) {
                AbortTransaction();
            }
        }
    }
}
