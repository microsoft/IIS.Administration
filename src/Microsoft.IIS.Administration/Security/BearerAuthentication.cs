// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Security {
    using AspNetCore.Authentication.JwtBearer;
    using AspNetCore.Builder;
    using Core.Security;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.Extensions.DependencyInjection;

    public static class BearerAuthenticationExtensions {

        public static IServiceCollection AddJwtBearerAuthentication(this IServiceCollection services, AuthenticationBuilder authBuilder)
        {
            var validator = new BearerTokenValidator((IApiKeyProvider)services.BuildServiceProvider().GetService(typeof(IApiKeyProvider)));

            authBuilder.AddJwtBearer(o => {

                //
                // Options
                o.Events = new JwtBearerEvents()
                {
                    OnMessageReceived = ctx =>
                    {
                        validator.OnReceivingToken(ctx);
                        return System.Threading.Tasks.Task.FromResult(0);
                    },
                    OnTokenValidated = ctx =>
                    {
                        validator.OnValidatedToken(ctx);
                        return System.Threading.Tasks.Task.FromResult(0);
                    }
                };

                o.SecurityTokenValidators.Add(validator);
            });

            return services;
        }
    }
}
