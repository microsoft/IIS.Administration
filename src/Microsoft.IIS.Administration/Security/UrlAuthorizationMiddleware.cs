// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Security {
    using System;
    using System.Threading.Tasks;
    using AspNetCore.Authorization;
    using AspNetCore.Http;
    using AspNetCore.Builder;
    using Newtonsoft.Json;
    using Core.Http;
    using Headers = Net.Http.Headers;
    

    public class UrlAuthorizationMiddleware {
        private readonly RequestDelegate _next;
        private readonly UrlAuthorizatonOptions _options;


        public UrlAuthorizationMiddleware(RequestDelegate next, UrlAuthorizatonOptions options) {
            if (next == null) {
                throw new ArgumentNullException(nameof(next));
            }

            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrEmpty(options.Path)) {
                throw new ArgumentNullException(nameof(options.Path));
            }

            _next = next;
            _options = options;
        }


        public async Task Invoke(HttpContext context, IAuthorizationService authorizationService) {

            if (context.Request.Path.StartsWithSegments(_options.Path)) {
                bool authorized;

                if (!string.IsNullOrEmpty(_options.PolicyName)) {
                    authorized = await authorizationService.AuthorizeAsync(context.User, null, _options.PolicyName);
                }
                else {
                    authorized = context.User?.Identity?.IsAuthenticated ?? false;
                }

                if (!authorized) {
                    var response = context.Response;

                    response.Headers.Add(Headers.HeaderNames.WWWAuthenticate, _options.AuthenticationScheme);

                    // 
                    // Not authorized, return HTTP 403 Forbidden instead of 401
                    // 401 breaks integrated host authentication
                    await context.Authentication.ForbidAsync(_options.AuthenticationScheme);

                    //
                    // Set proper response
                    response.ContentType = JsonProblem.CONTENT_TYPE;
                    response.Headers[Headers.HeaderNames.ContentLanguage] = JsonProblem.CONTENT_LANG;

                    object error = Administration.ErrorHelper.UnauthorizedError(_options.AuthenticationScheme);
                    await response.WriteAsync(JsonConvert.SerializeObject(error));

                    return;
                }
            }

            //
            // Allow to continue
            await _next(context);
        }
    }


    public class UrlAuthorizatonOptions {
        public PathString Path { get; set; } = "/";
        public string PolicyName { get; set; }
        public string AuthenticationScheme { get; set; }
    }


    public static class UrlAuthorizationExtensions {
        public static IApplicationBuilder UseUrlAuthorization(this IApplicationBuilder builder, UrlAuthorizatonOptions options) {
            return builder.UseMiddleware<UrlAuthorizationMiddleware>(options);
        }
    }
}
