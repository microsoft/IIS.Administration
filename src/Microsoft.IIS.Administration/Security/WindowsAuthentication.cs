// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Security {
    using AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using System.Security.Principal;


    public static class WindowsAuthenticationExtensions {

        public static IApplicationBuilder UseWindowsAuthentication(this IApplicationBuilder builder) {
            var config = (IConfiguration) builder.ApplicationServices.GetService(typeof(IConfiguration));
            bool requireWindowsAuth = config.GetSection("security")?.GetValue("require_windows_authentication", true) ?? true;

            if (!requireWindowsAuth) {
                return builder;
            }

            return builder.Use(async (ctx, next) => {
                bool sendChallenge = true;
                foreach (var identity in ctx.User.Identities) {
                    if (identity is WindowsIdentity && identity.IsAuthenticated) {
                        sendChallenge = false;
                        break;
                    }
                }

                if (sendChallenge) {
                    Challenge(ctx.Response);
                }
                else {
                    await next.Invoke();
                }
            });
        }

        private static void Challenge(HttpResponse response) {
            response.StatusCode = StatusCodes.Status401Unauthorized;
        }
    }
}
