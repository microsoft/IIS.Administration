// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Security {
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using AspNetCore.Builder;
    using Core.Security;
    using Microsoft.Extensions.DependencyInjection;

    using System;

    using System.Collections.Generic;

    using System.IO;

    using Microsoft.AspNetCore.Authentication;

    using Microsoft.AspNetCore.Authentication.JwtBearer;

    using Microsoft.AspNetCore.Builder;

    using Microsoft.AspNetCore.Hosting;

    using Microsoft.AspNetCore.Http;

    using Microsoft.Extensions.Configuration;

    using Microsoft.Extensions.DependencyInjection;

    using Microsoft.Net.Http.Headers;

    using Newtonsoft.Json.Linq;

    public static class BearerAuthenticationExtensions {

        public static void UseBearerAuthentication(this IServiceCollection services) {
            // var validator = new BearerTokenValidator((IApiKeyProvider)builder.ApplicationServices.GetService(typeof(IApiKeyProvider)));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.Events = new JwtBearerEvents()
                {
                    OnMessageReceived = ctx =>
                    {
                        ctx.HttpContext.RequestServices.GetRequiredService<BearerTokenValidator>().OnReceivingToken(ctx);
                        return System.Threading.Tasks.Task.FromResult(0);
                    },
                    OnTokenValidated = ctx =>
                    {
                        ctx.HttpContext.RequestServices.GetRequiredService<BearerTokenValidator>().OnValidatedToken(ctx);
                        return System.Threading.Tasks.Task.FromResult(0);
                    }
                };
            });
            options.SecurityTokenValidators.Add(validator);

            return builder.UseJwtBearerAuthentication(options);
        }
    }
}
