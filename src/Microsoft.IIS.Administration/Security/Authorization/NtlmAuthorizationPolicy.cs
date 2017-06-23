// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Security.Authorization {
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Headers = Net.Http.Headers;
    using Microsoft.IIS.Administration.Core.Http;
    using Newtonsoft.Json;



    sealed class NtlmAuthorizationPolicy : IAssertAuthorizationRequirement, IAuthorizationPolicy {

        public NtlmAuthorizationPolicy(string roles, RoleMapping mapping) {
            Assert = (ctx) => {
                foreach (var r in roles.Split(',')) {
                    string role = r.Trim();
                    if (!string.IsNullOrEmpty(role) && mapping.IsUserInRole(ctx.User, role)) {
                        return true;
                    }
                }
                return false;
            };
        }

        public Func<AuthorizationHandlerContext, bool> Assert { get; private set; }

        public async Task Challenge(HttpContext ctx) {
            HttpResponse response = ctx.Response;

            //
            // Set HTTP 401
            // The WebHost should set the supported WWW-Authenticate values
            response.StatusCode = StatusCodes.Status401Unauthorized;

            //
            // Write response
            response.ContentType = JsonProblem.CONTENT_TYPE;
            response.Headers[Headers.HeaderNames.ContentLanguage] = JsonProblem.CONTENT_LANG;

            object error = Administration.ErrorHelper.UnauthorizedError("Negotiate,NTLM", response.StatusCode);
            await response.WriteAsync(JsonConvert.SerializeObject(error));
        }
    }
}
