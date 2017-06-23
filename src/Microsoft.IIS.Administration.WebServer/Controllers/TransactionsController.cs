// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer
{
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using System.Collections.Generic;
    using System.Linq;


    [RequireWebServer]
    public class TransactionsController : ApiBaseController
    {
        private IApplicationHostConfigProvider _configProvider;

        public TransactionsController(IApplicationHostConfigProvider configProvider) {
            _configProvider = configProvider;
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.TransactionsName)]
        public object Get()
        {
            var transaction = Store.Transaction;
            List<Transaction> transactions = new List<Transaction>();
            if(transaction != null) {
                transactions.Add(transaction);
            }
            return new {
                transactions = transactions.Select(trans => TransactionHelper.ToJsonObject(trans))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.TransactionName)]
        public object Get(string id)
        {
            Transaction activeTransaction = Store.Transaction;
            if (activeTransaction == null || id != activeTransaction.Id) {
                return NotFound();
            }

            return TransactionHelper.ToJsonObject(activeTransaction);
        }

        [HttpPost]
        [Audit]
        [ResourceInfo(Name = Defines.TransactionName)]
        public object Post()
        {
            Transaction transaction = Store.BeginTransaction(_configProvider);

            //
            // Set the current management unit
            Context.SetManagementUnit(Store.ManagementUnit);

            //
            // Create response
            dynamic tran = (dynamic)TransactionHelper.ToJsonObject(transaction);

            return Created((string)TransactionHelper.GetLocation(tran.id), tran);
        }

        [HttpPatch]
        [Audit]
        public object Patch(string id, [FromBody] PatchModel model)
        {
            Transaction activeTransaction = Store.Transaction;
            if (activeTransaction == null || id != activeTransaction.Id) {
                return NotFound();
            }
            //Patch without specifying any changes is going to return the same as Get
            if (model == null || model.state == null) {
                return TransactionHelper.ToJsonObject(activeTransaction);
            }
            switch (model.state) {
                case TransactionState.Committed:
                    Store.CommitTransaction();
                    activeTransaction.State = TransactionState.Committed;
                    break;
                case TransactionState.Aborted: //Same logic as committed but doesn't commit the server manager
                    Store.AbortTransaction();
                    activeTransaction.State = TransactionState.Aborted;
                    break;
                default:
                    break;
            }
            return TransactionHelper.ToJsonObject(activeTransaction);
        }
    }

    public class PatchModel
    {
        public TransactionState? state;
    }
}
