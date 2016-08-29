// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Security {
    using AspNetCore.Http;
    using System.Threading.Tasks;
    using Extensions.Primitives;
    using System.Net;
    using Core.Http;
    using Newtonsoft.Json;


    public class SSLCheck {
        RequestDelegate _next;

        public SSLCheck(RequestDelegate next) {
            _next = next;
        }

        public async Task Invoke(HttpContext context) {
            StringValues values;

            if (context.Request.IsHttps || context.Request.Headers.TryGetValue(HeaderNames.X_Forwarded_Proto, out values) &&
                values.Count > 0 &&
                values[0] == "https") {

                //
                // SSL is provided. Allow to continue
                await _next(context);
            }
            else {
                //
                // SSL not provided
                // Request Upgrade
                context.Response.Clear();

                context.Response.ContentType = JsonProblem.CONTENT_TYPE;
                context.Response.Headers[Net.Http.Headers.HeaderNames.ContentLanguage] = JsonProblem.CONTENT_LANG;
                context.Response.Headers[Net.Http.Headers.HeaderNames.Upgrade] = "TLS/1.0, HTTP/1.1";

                context.Response.StatusCode = (int)HttpStatusCode.UpgradeRequired;

                await context.Response.WriteAsync(JsonConvert.SerializeObject(SslRequiredError()));
            }
        }

        private static object SslRequiredError() {
            return new {
                title = "Upgrade",
                detail = "Requires HTTPS",
                status = (int)HttpStatusCode.UpgradeRequired
            };
        }

    }
}
