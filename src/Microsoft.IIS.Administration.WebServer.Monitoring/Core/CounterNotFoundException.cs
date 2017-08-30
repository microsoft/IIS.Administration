// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Monitoring
{
    using System;

    public class CounterNotFoundException : Exception
    {
        public CounterNotFoundException(IPerfCounter counter, string message = null) : base(message)
        {
        }
    }
}
