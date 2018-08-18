// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Security.Authorization {
    using System;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Configuration;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Server.HttpSys;

    sealed class AuthorizationPolicyManager {
        private const string WINDOWS_USER = "WindowsUser";
        private const string AUTHENTICATED_USER = "AuthenticatedUser";
        private const string ACCESS_KEY = "AccessKey";
        private const string READ_ONLY = "ReadOnly";
        private const string FORBIDDEN = "Forbidden";


        private RoleMapping _roleMapping;
        private AccessPolicyOptions _options;

        public AuthorizationPolicyManager(IConfiguration config) {
            if (config == null) {
                throw new ArgumentNullException(nameof(config));
            }

            _roleMapping = new RoleMapping(config);
            _options = new AccessPolicyOptions(config);
        }

        public void Configure(AuthorizationOptions o) {
            //
            // Windows User
            o.AddPolicy(WINDOWS_USER, p => p.AddAuthenticationSchemes(HttpSysDefaults.AuthenticationScheme).RequireAuthenticatedUser());


            //
            // Authenticated User
            o.AddPolicy(AUTHENTICATED_USER, p => p.RequireAuthenticatedUser());


            //
            // Access Key
            o.AddPolicy(ACCESS_KEY, p => {
                p.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                 .RequireAuthenticatedUser()
                 .AddRequirements(new BearerAuthorizationPolicy());
            });


            //
            // Read Only 
            o.AddPolicy(READ_ONLY, p => p.AddRequirements(new ReadOnlyPolicy()));


            //
            // Forbidden
            o.AddPolicy(FORBIDDEN, p => p.AddRequirements(new ForbiddenPolicy()));


            //
            // Api policy
            o.AddPolicy("Api", p => {
                p.RequireAuthenticatedUser();
                AddAccessPolicy(p, o, _options.Api);
            });


            //
            // ApiKeys policy
            o.AddPolicy("ApiKeys", p => {
                p.RequireAuthenticatedUser();
                AddAccessPolicy(p, o, _options.ApiKeys);
            });


            //
            // System policy
            o.AddPolicy("System", p => {
                p.RequireAuthenticatedUser();
                AddAccessPolicy(p, o, _options.System);
            });
        }

        private void AddAccessPolicy(AuthorizationPolicyBuilder builder, AuthorizationOptions options, AccessPolicy policy) {
            if (policy == null) {
                throw new ArgumentNullException(nameof(policy));
            }

            //
            // Forbidden
            if (policy.Forbidden) {
                builder.Combine(GetPolicyRequirement(FORBIDDEN, options));
            }

            //
            // AccessKey
            if (policy.AccessKey) {
                builder.Combine(GetPolicyRequirement(ACCESS_KEY, options));
            }

            //
            // Users
            if (!policy.Users.Equals("Everyone", StringComparison.OrdinalIgnoreCase)) {
                builder.Combine(GetPolicyRequirement(WINDOWS_USER, options));

                builder.AddRequirements(new NtlmAuthorizationPolicy(policy.Users, _roleMapping));
            }
            else {
                builder.Combine(GetPolicyRequirement(AUTHENTICATED_USER, options));
            }

            //
            // Read Only
            if (policy.ReadOnly) {
                builder.Combine(GetPolicyRequirement(READ_ONLY, options));
            }
        }

        private static AuthorizationPolicy GetPolicyRequirement(string policy, AuthorizationOptions options) {
            return options.GetPolicy(policy) ?? throw new Exception($"{policy} policy not found");
        }
    }
}
