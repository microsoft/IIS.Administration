// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Monitoring
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    interface ICounterProvider
    {
        Task<IEnumerable<IPerfCounter>> GetCounters(string category, string instance);

        Task<IEnumerable<IPerfCounter>> GetSingletonCounters(string category);
    }
}
