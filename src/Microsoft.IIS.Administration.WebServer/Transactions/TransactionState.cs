// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer
{
    public enum TransactionState
    {
        Started,
        Committed,
        Aborted,
        TimedOut
    }
}
