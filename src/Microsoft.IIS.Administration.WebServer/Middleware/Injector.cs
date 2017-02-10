// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer
{
    using AspNetCore.Http;
    using Core;
    using System;
    using System.Threading.Tasks;

    public class Injector
    {
        private RequestDelegate _next;
        private static readonly string WEBSERVER_PATH = "/" + Defines.PATH;
        private IApplicationHostConfigProvider _confgProvider;

        public Injector(RequestDelegate next, IApplicationHostConfigProvider provider)
        {
            _next = next;
            _confgProvider = provider;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!IsWebServerRequest(context)) {
                await _next(context);
                return;
            }

            if (context.GetTransactionId() != null) {
                await ProceedTransaction(context);
            }
            else {
                await ProceedNoTransaction(context);
            }
        }



        private static bool IsWebServerRequest(HttpContext context)
        {
            // Transaction processing is limited to webserver
            return context.Request.Path.StartsWithSegments(WEBSERVER_PATH);
        }

        private async Task ProceedTransaction(HttpContext context)
        {
            string transactionId = context.GetTransactionId();

            //RefreshActiveTransaction will defer the timeout for the transaction if it returns a non null transaction
            var activeTransaction = Store.KeepAliveTransaction();

            if (activeTransaction == null || !activeTransaction.Id.Equals(transactionId)) {
                throw new NotFoundException("transaction");
            }

            if (Store.ManagementUnit == null) {
                throw new Exception("Management unit not found");
            }

            //Set the IManagementUnit stored in the context to be the TransactionManagementUnit
            context.SetManagementUnit(Store.ManagementUnit);

            await _next(context);
        }

        private async Task ProceedNoTransaction(HttpContext context)
        {
            using (var mu = new MgmtUnit(_confgProvider)) {
                context.SetManagementUnit(mu);
                try {
                    await _next(context);
                }
                finally {
                    context.SetManagementUnit(null);
                }
            }
        }
    }
}
