// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Security.Authorization {
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.IIS.Administration.Core.Http;
    

    sealed class AuthorizationHandler : AuthorizationHandler<IAssertAuthorizationRequirement> {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext ctx, IAssertAuthorizationRequirement requirement) {
            HttpHelper.Current.SetAuthorizationHandlerContext(ctx);

            if (requirement.Assert(ctx)) {
                ctx.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
