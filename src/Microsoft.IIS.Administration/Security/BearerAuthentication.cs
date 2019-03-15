// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Security {
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Core.Security;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.IIS.Administration.Core;
    using Microsoft.AspNetCore.Server.HttpSys;

    public static class BearerAuthenticationExtensions {

        public static IServiceCollection AddBearerAuthentication(this IServiceCollection services, Action<ApiKeyOptions> configure = null)
        {
            var opts = new ApiKeyOptions();

            if (configure != null)
            {
                configure.Invoke(opts);
            }

            IHostingEnvironment env = services.BuildServiceProvider().GetRequiredService<IHostingEnvironment>();
            IConfiguration config = services.BuildServiceProvider().GetRequiredService<IConfiguration>();

            var provider = new ApiKeyProvider(opts);

            provider.UseStorage(new ApiKeyFileStorage(env.GetConfigPath("api-keys.json")));
            provider.UseStorage(new ApiKeyConfigStorage(config));

            var validator = new BearerTokenValidator(provider);

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = HttpSysDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.SecurityTokenValidators.Add(validator);

                options.Events = new JwtBearerEvents()
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
            });

            return services.AddSingleton<IApiKeyProvider>(provider);
        }
    }
}
