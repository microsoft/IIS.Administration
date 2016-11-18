// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer
{
    using Core;
    using System;

    public class Defines
    {
        private const string ENDPOINT = "webserver";
        private const string TRANSACTIONS_ENDPOINT = "transactions";

        public const string ResourceName = "Microsoft.WebServer";
        public static readonly string PATH = $"{Globals.API_PATH}/{ENDPOINT}";
        public static readonly ResDef Resource = new ResDef("webserver", new Guid("29EAAB97-AC52-4840-B258-85C0B7125966"), ENDPOINT);

        public const string TransactionsName = "Microsoft.WebServer.Transactions";
        public const string TransactionName = "Microsoft.WebServer.Transaction";
        public static readonly string TRANSACTIONS_PATH = $"{PATH}/{TRANSACTIONS_ENDPOINT}";
        public static readonly ResDef TransactionsResource = new ResDef("transactions", new Guid("E3396B3C-8053-4916-BFD1-91F0CDB8581B"), TRANSACTIONS_ENDPOINT);

        public const string TRANSACTION_HEADER = "Transaction-Id";
    }
}
