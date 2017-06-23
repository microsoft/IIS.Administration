// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Security.Authorization {
    using System;
    using System.Threading.Tasks;
    using AspNetCore.Authorization;
    using AspNetCore.Http;
    using Microsoft.IIS.Administration.Core;
    using System.Linq;


    sealed class AuthorizationPolicyMiddleware {
        private readonly RequestDelegate _next;


        public AuthorizationPolicyMiddleware(RequestDelegate next) {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }


        public async Task Invoke(HttpContext context, IAuthorizationService authorizationService) {
            HttpResponse response = context.Response;

            try {
                await _next(context);
            }
            catch (UnauthorizedArgumentException) {
                response.Clear();
                response.StatusCode = StatusCodes.Status403Forbidden;
            }

            //
            // Handle only 401 and 403
            if (response.StatusCode != StatusCodes.Status403Forbidden &&
                response.StatusCode != StatusCodes.Status401Unauthorized) {
                return;
            }

            //
            // Get uncompleted AuthorizationPolicy
            AuthorizationHandlerContext azContext = context.GetAuthorizationHandlerContext();
            IAuthorizationPolicy policy = (IAuthorizationPolicy)azContext?.PendingRequirements.Where(r => r is IAuthorizationPolicy).FirstOrDefault() ?? null;

            //
            // Do Challenge
            if (policy != null) {
                await policy.Challenge(context);
            }
        }
    }
}
