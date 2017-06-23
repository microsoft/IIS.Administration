// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Security.Authorization {
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;


    sealed class ForbiddenPolicy : IAssertAuthorizationRequirement, IAuthorizationPolicy {

        public ForbiddenPolicy() {
            Assert = _=> false;
        }

        public Func<AuthorizationHandlerContext, bool> Assert { get; private set; }

        public Task Challenge(HttpContext ctx) {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;

            return Task.CompletedTask;
        }
    }
}
