// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer
{
    using System;

    public class Transaction
    {
        private const string PURPOSE = "transaction";
       
        public Transaction()
        {
            Id = Core.Utils.Uuid.Encode(Guid.NewGuid().ToString().Substring(0, 10), PURPOSE);
            CreatedOn = DateTime.UtcNow;
        }

        public string Id { get; private set; }
        public DateTime CreatedOn { get; private set; }
        public DateTime ExpiresOn { get; set; }
        public TransactionState State { get; set; }
    }
}
