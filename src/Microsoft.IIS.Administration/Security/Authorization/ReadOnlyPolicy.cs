// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Security.Authorization {
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.IIS.Administration.Core.Http;


    sealed class ReadOnlyPolicy : IAssertAuthorizationRequirement, IAuthorizationPolicy {

        public ReadOnlyPolicy() {
            Assert = _=> {
                string verb = HttpHelper.Current.Request.Method.ToUpper();
                return verb == "GET" || verb == "HEAD" || verb == "OPTIONS";
            };
        }

        public Func<AuthorizationHandlerContext, bool> Assert { get; private set; }

        public Task Challenge(HttpContext ctx) {
            ctx.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;

            return Task.CompletedTask;
        }
    }
}
