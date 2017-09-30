// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Monitoring
{
    using System;
    using System.Collections.Generic;

    public class MissingCountersException : Exception
    {
        public MissingCountersException(IEnumerable<IPerfCounter> counters, string message = null) : base(message)
        {
            Counters = counters;
        }

        public IEnumerable<IPerfCounter> Counters { get; private set; }
    }
}
