// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Security {
    using AspNetCore.Authentication.JwtBearer;
    using AspNetCore.Builder;
    using Core.Security;

    public static class BearerAuthenticationExtensions {

        public static IApplicationBuilder UseBearerAuthentication(this IApplicationBuilder builder) {
            var validator = new BearerTokenValidator((IApiKeyProvider)builder.ApplicationServices.GetService(typeof(IApiKeyProvider)));

            //
            // Options
            var options = new JwtBearerOptions() {
                AutomaticAuthenticate = true,
                AutomaticChallenge = true,
                Events = new JwtBearerEvents() {
                    OnMessageReceived = ctx => {
                        validator.OnReceivingToken(ctx);
                        return System.Threading.Tasks.Task.FromResult(0);
                    },
                    OnTokenValidated = ctx => {
                        validator.OnValidatedToken(ctx);
                        return System.Threading.Tasks.Task.FromResult(0);
                    }
                }
            };

            options.SecurityTokenValidators.Add(validator);

            return builder.UseJwtBearerAuthentication(options);
        }
    }
}
