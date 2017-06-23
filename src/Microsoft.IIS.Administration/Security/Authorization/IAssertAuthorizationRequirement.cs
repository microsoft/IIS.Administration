// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Security.Authorization {
    using System;
    using Microsoft.AspNetCore.Authorization;

    
    interface IAssertAuthorizationRequirement : IAuthorizationRequirement {
        Func<AuthorizationHandlerContext, bool> Assert { get; }
    }
}
