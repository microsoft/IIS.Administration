// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Cors
{
    using AspNetCore.Builder;
    using AspNetCore.Http;
    using Core.Http;
    using Extensions.Configuration;
    using Extensions.DependencyInjection;
    using Extensions.Primitives;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;

    public static class CorsExtensions
    {
        public static IApplicationBuilder UseCrossOrigin(this IApplicationBuilder builder, string rootPath)
        {
            //
            // Allow CORs for rootPath only
            if (!string.IsNullOrEmpty(rootPath) && rootPath != "/") {
                builder.Use(async (context, next) => {

                    bool isCorsPreflight = context.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase) &&
                                           context.Request.Headers[HeaderNames.Origin].Any();

                    if (isCorsPreflight && !context.Request.Path.StartsWithSegments(rootPath)) {
                        context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                    }
                    else {
                        await next.Invoke();
                    }
                });
            }

            //
            // Setup 
            var config = builder.ApplicationServices.GetRequiredService<IConfiguration>();
            var corsConfiguration = new CorsConfiguration(config);

            builder.UseCors(cBuilder => {
                cBuilder.AllowAnyHeader();
                cBuilder.WithExposedHeaders(HeaderNames.Total_Count,
                                            HeaderNames.AcceptRanges,
                                            HeaderNames.ContentRange,
                                            Net.Http.Headers.HeaderNames.Location,
                                            Net.Http.Headers.HeaderNames.Allow,
                                            Net.Http.Headers.HeaderNames.WWWAuthenticate);
                cBuilder.WithMethods("GET","HEAD","POST","PUT","PATCH","DELETE","OPTIONS","DEBUG");
                cBuilder.AllowCredentials();

                IEnumerable<string> allowedOrigins = GetAllowedOrigins(corsConfiguration);

                if (allowedOrigins.Any(o=> o.Equals("*")))
                {
                    cBuilder.AllowAnyOrigin();
                }
                else
                {
                    cBuilder.WithOrigins(allowedOrigins.ToArray());
                }
            });

            // We must allow OPTIONS to enter the application without integrated host authentication
            // We do not want OPTIONS methods to ever pass cors middleware
            builder.Use( async (context, next) => {

                if(!context.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase)) {
                    try {                        
                        await next.Invoke();
                    }
                    catch {
                        //
                        // In case of unhandled exception, ASP.NET clears the response
                        // We want to make sure that we add back the CORS headers
                        RestoreCors(context.Response);
                        throw;
                    }
                }
            });

            return builder;
        }

        private static IEnumerable<string> GetAllowedOrigins(CorsConfiguration config)
        {
            return config.Rules.Where(r => r.Allow).Select(r => r.Origin);
        }

        private static void RestoreCors(HttpResponse response)
        {
            const string CORS_PREFIX = "Access-Control-";

            // Capture CORS headers
            var corsHeaders = new List<KeyValuePair<string, StringValues>>();
            foreach (var h in response.Headers) {
                if (h.Key.StartsWith(CORS_PREFIX, StringComparison.OrdinalIgnoreCase)) {
                    corsHeaders.Add(h);
                }
            }

            if (corsHeaders.Count > 0) {
                response.OnStarting(() => {
                    // Restore CORS headers, if not present
                    if (!response.Headers.Any(h => h.Key.StartsWith(CORS_PREFIX, StringComparison.OrdinalIgnoreCase))) {
                        foreach (var corsHeader in corsHeaders) {
                            response.Headers.Add(corsHeader);
                        }
                    }
                    return Task.CompletedTask;
                });
            }
        }
    }
}
