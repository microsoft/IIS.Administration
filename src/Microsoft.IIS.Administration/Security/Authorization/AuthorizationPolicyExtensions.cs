// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Security.Authorization {
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;


    static class AuthorizationPolicyExtensions {
        const string AuthorizationContext_Key = "Authorization:AuthorizationHandlerContext";

        public static IServiceCollection AddAuthorizationPolicy(this IServiceCollection services) {
            var config = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
            var policy = new AuthorizationPolicyManager(config);

            return services.AddAuthorization(o => policy.Configure(o))
                           .AddSingleton<IAuthorizationHandler, AuthorizationHandler>();
        }

        public static IApplicationBuilder UseAuthorizationPolicy(this IApplicationBuilder builder) {
            return builder.UseMiddleware<AuthorizationPolicyMiddleware>();
        }

        public static AuthorizationHandlerContext GetAuthorizationHandlerContext(this HttpContext ctx) {
            return (AuthorizationHandlerContext)ctx.Items[AuthorizationContext_Key];
        }

        public static void SetAuthorizationHandlerContext(this HttpContext ctx, AuthorizationHandlerContext authorizationContext) {
            ctx.Items[AuthorizationContext_Key] = authorizationContext;
        }

    }
}
