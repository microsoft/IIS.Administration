// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer {
    using System;


    public static class TransactionHelper {
        public static dynamic ToJsonObject(Transaction t) {
            var obj = new {
                id = t.Id,
                created_on = t.CreatedOn,
                expires_on = t.ExpiresOn,
                state = Enum.GetName(typeof(TransactionState), t.State)
            };

            return Core.Environment.Hal.Apply(Defines.TransactionsResource.Guid, obj);
        }

        public static string GetLocation(string id) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentNullException(nameof(id));
            }

            return $"/{Defines.TRANSACTIONS_PATH}/{id}";
        }
    }
}
