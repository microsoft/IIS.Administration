// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Security.Authorization {
    using System;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Headers = Net.Http.Headers;
    using System.Threading.Tasks;
    using Microsoft.IIS.Administration.Core.Http;
    using Newtonsoft.Json;



    sealed class BearerAuthorizationPolicy : IAssertAuthorizationRequirement, IAuthorizationPolicy {
        public BearerAuthorizationPolicy() {
            Assert = ctx => ctx.User.HasClaim(c => c.Type == Core.Security.ClaimTypes.AccessToken);
        }

        public Func<AuthorizationHandlerContext, bool> Assert { get; private set; }

        public async Task Challenge(HttpContext ctx) {
            HttpResponse response = ctx.Response;

            if (!response.Headers.Keys.Contains(Headers.HeaderNames.WWWAuthenticate)) {
                response.Headers.Append(Headers.HeaderNames.WWWAuthenticate, JwtBearerDefaults.AuthenticationScheme);
            }

            response.StatusCode = StatusCodes.Status403Forbidden;

            response.ContentType = JsonProblem.CONTENT_TYPE;
            response.Headers[Headers.HeaderNames.ContentLanguage] = JsonProblem.CONTENT_LANG;

            object error = Administration.ErrorHelper.UnauthorizedError(JwtBearerDefaults.AuthenticationScheme, response.StatusCode);
            await response.WriteAsync(JsonConvert.SerializeObject(error));
        }
    }
}
