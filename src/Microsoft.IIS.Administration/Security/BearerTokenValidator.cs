// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Security {
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;
    using Core.Http;
    using Core.Security;
    using AspNetCore.Authentication.JwtBearer;
    using IdentityModel.Tokens;



    public class BearerTokenValidator : ISecurityTokenValidator {
        private IApiKeyProvider _keyProvider;


        public BearerTokenValidator(IApiKeyProvider keyProvider) {
            _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
        }

        public bool CanValidateToken {
            get {
                throw new NotImplementedException();
            }
        }

        public int MaximumTokenSizeInBytes {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public bool CanReadToken(string securityToken) {
            // 
            // Basic check
            // ValidateToken will perform extensive validation
            return !string.IsNullOrWhiteSpace(securityToken);
        }

        public ClaimsPrincipal ValidateToken(string securityToken, 
                                            TokenValidationParameters validationParameters, 
                                            out IdentityModel.Tokens.SecurityToken validatedToken) {
            ApiKey key = null;

            // Look up api-key
            try {
                key = _keyProvider.FindKey(securityToken);
            }
            catch {
                //
                // Failure to obtain the key is considered as invalid/missing key
            }

            //
            // The api-key is not found, so the validation's failed.
            if (key == null) {
                validatedToken = null;

                // Unauthenticated Principal
                return new ClaimsPrincipal(); 
            }

            //
            // Success!
            validatedToken = new SecurityToken(key);

            // Authenticated Principal
            IEnumerable<Claim> claims = new Claim[] { new Claim(Core.Security.ClaimTypes.AccessToken, securityToken) };
            return new ClaimsPrincipal(new ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme));
        }


        public void OnReceivingToken(MessageReceivedContext ctx) {
            string token = null;
            //
            // Try get from request header
            string accessToken = ctx.Request.Headers[HeaderNames.Access_Token];

            //
            // Ensure the token is provided
            if (string.IsNullOrEmpty(accessToken)) {
                return;
            }

            //
            // Parse the access token
            // Access-Token: Bearer <token>
            //
            if (accessToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) {
                token = accessToken.Substring("Bearer ".Length).Trim();
            }

            if (!string.IsNullOrEmpty(token)) {
                // ValidateToken will determine later if the provided token can be used
                ctx.Token = token;
            }
        }

        public void OnValidatedToken(TokenValidatedContext ctx) {
            /*
            // 
            // Join identities if successfully authenticated
            if (ctx.Ticket?.Principal?.Identity?.IsAuthenticated == true) {
                if (ctx.HttpContext.User != null) {
                    ctx.HttpContext.User.AddIdentities(ctx.Ticket.Principal.Identities);
                }
                else {
                    ctx.HttpContext.User = ctx.Ticket.Principal;
                }
            }
            */
        }
    }
}
