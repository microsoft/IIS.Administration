// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Security.Authorization {
    using System;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.Extensions.Configuration;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using System.Collections.Generic;



    sealed class AuthorizationPolicy {
        private RoleMapping _roleMapping;
        private AccessPolicyOptions _options;

        public AuthorizationPolicy(IConfiguration config) {
            if (config == null) {
                throw new ArgumentNullException(nameof(config));
            }

            _roleMapping = new RoleMapping(config);
            _options = new AccessPolicyOptions(config);
        }

        public void Configure(AuthorizationOptions o) {
            //
            // Users
            o.AddPolicy("Users", p => p.AddAuthenticationSchemes("Negotiate", "NTLM").RequireAuthenticatedUser());


            //
            // AccessKey
            o.AddPolicy("AccessKey", p => {
                p.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                 .RequireAuthenticatedUser()
                 .AddRequirements(new BearerAuthorizationPolicy());
            });


            //
            // ReadOnly 
            o.AddPolicy("ReadOnly", p => p.AddRequirements(new ReadOnlyPolicy()));


            //
            // Forbidden
            o.AddPolicy("Forbidden", p => p.AddRequirements(new ForbiddenPolicy()));


            //
            // Api policy
            o.AddPolicy("Api", p => {
                p.RequireAuthenticatedUser();
                AddRequirements(p, o, _options.Api);
            });


            //
            // ApiKeys policy
            o.AddPolicy("ApiKeys", p => {
                p.RequireAuthenticatedUser();
                AddRequirements(p, o, _options.ApiKeys);
            });


            //
            // System policy
            o.AddPolicy("System", p => {
                p.RequireAuthenticatedUser();
                AddRequirements(p, o, _options.System);
            });
        }

        private void AddRequirements(AuthorizationPolicyBuilder builder, AuthorizationOptions options, IEnumerable<string> requirements) {
            foreach (var r in requirements) {
                var values = r.Split(':');
                string name = values[0];

                //
                // Add policy
                builder.Combine(options.GetPolicy(name) ?? throw new Exception($"{name} requirement not found"));

                if (values.Length == 1) {
                    return;
                }

                //
                // Users
                if (name.Equals("Users", StringComparison.OrdinalIgnoreCase)) {
                    builder.AddRequirements(new NtlmAuthorizationPolicy(values[1], _roleMapping));
                }
            }
        }
    }
}
