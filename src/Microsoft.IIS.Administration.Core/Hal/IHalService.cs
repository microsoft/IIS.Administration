// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core
{
    using System;
    using System.Collections.Generic;

    public interface IHalService
    {
        void ProvideLink(Guid resourceId, string name, Func<dynamic, dynamic> func);
        object Apply(Guid resourceId, object obj, bool all = true);
        IDictionary<string, dynamic> Get(Guid resourceId, object obj, bool all = true);
    }

    public interface IConditionalHalService : IHalService
    {
        void ProvideLink(Guid resourceId, string name, Func<dynamic, dynamic> func, Func<dynamic, bool> condition);
    }
}
