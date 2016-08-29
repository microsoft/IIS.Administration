// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Transactions
{
    using AspNetCore.Http;
    using Core;
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class Middleware
    {
        private RequestDelegate _next;
        private static readonly string WEBSERVER_PATH = "/" + WebServer.Defines.PATH;
        internal static ReaderWriterLockSlim syncLock = new ReaderWriterLockSlim();

        public Middleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (CanSkipTransaction(context)) {
                await _next(context);
                return;
            }
            //If we specify a transaction that is not the active transaction that is a problem.

            //If request has transaction header check if transaction exists
            //If the transaction exists set up the TransactionManagementUnit with the correct ServerManager
            //Else return an error because a non existant transaction was specified
            if (GetTransactionId(context) != null) {
                syncLock.EnterWriteLock();
                try {
                    await ProceedTransaction(context);
                }
                finally {
                    syncLock.ExitWriteLock();
                }
            }
            else {
                //We use enter read lock even though we will be performing changes.
                //we do this because this type of lock provides us with the multi threading pattern we want. Exclusive access to one code path, shared access to another code path.
                syncLock.EnterReadLock();
                try {
                    await ProceedNoTransaction(context);
                }
                finally {
                    syncLock.ExitReadLock();
                }
            }
        }

        private static bool CanSkipTransaction(HttpContext context)
        {
            // Transaction processing is limited to webserver
            if(!context.Request.Path.StartsWithSegments(WEBSERVER_PATH)) {
                return true;
            }

            string requestMethod = context.Request.Method.ToUpper();
            switch (requestMethod) {
                case "POST":
                case "PATCH":
                case "DELETE":
                case "PUT":
                    return false;
                default:
                    return GetTransactionId(context) == null;
            }
        }


        private static string GetTransactionId(HttpContext context)
        {
            string transactioId = context.Request.Headers[Defines.TRANSACTION_HEADER];
            return string.IsNullOrEmpty(transactioId) ? null : transactioId;
        }

        private async Task ProceedTransaction(HttpContext context)
        {
            string transactionId = GetTransactionId(context);

            //RefreshActiveTransaction will defer the timeout for the transaction if it returns a non null transaction
            var activeTransaction = Store.KeepAliveTransaction();

            if (activeTransaction == null) {
                throw new Exception("There is no active transaction.");
            }

            if (!activeTransaction.Id.Equals(transactionId)) {
                throw new Exception("Unexpected active transaction");
            }

            if (Store.ManagementUnit == null) {
                throw new Exception("Management unit not found");
            }

            //Set the IManagementUnit stored in the context to be the TransactionManagementUnit
            context.SetManagementUnit(Store.ManagementUnit);

            await _next(context);
          
            
            //If request has transaction header we do not commit on the way out
        }

        private async Task ProceedNoTransaction(HttpContext context)
        {
            //We cannot let a one - off request interfere when there is an active transaction
            if (Store.Transaction != null) {
                //TODO return error page/message
                throw new NotFoundException("transaction");
                //context.Response.WriteAsync()
            }

            await _next(context);
        }
    }
}
