// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer
{
    using AspNetCore.Http;
    using System.Threading.Tasks;

    public class Injector
    {
        private RequestDelegate _next;
        private static readonly string WEBSERVER_PATH = "/" + Defines.PATH;

        public Injector(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context ) {

            bool webServerTargetted = context.Request.Path.StartsWithSegments(WEBSERVER_PATH);

            // Don't inject management unit if one already exists or the request isn't targetting the webserver
            if (context.GetManagementUnit() != null || !webServerTargetted) {
                await _next(context);
            }
            else {
                using (var mu = new MgmtUnit()) {
                    context.SetManagementUnit(mu);
                    await _next(context);
                    context.SetManagementUnit(null);
                }
            }
        }
    }
}
