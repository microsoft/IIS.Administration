// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Transactions
{
    using Core;
    using System;

    public class Defines
    {
        private const string ENDPOINT = "transactions";

        public const string TransactionsName = "Microsoft.WebServer.Transactions";
        public const string TransactionName = "Microsoft.WebServer.Transaction";
        public static readonly string PATH = $"{WebServer.Defines.PATH}/{ENDPOINT}";
        public static readonly ResDef Resource = new ResDef("transactions", new Guid("E3396B3C-8053-4916-BFD1-91F0CDB8581B"), ENDPOINT);

        public const string TRANSACTION_HEADER = "Transaction-Id";
    }
}
