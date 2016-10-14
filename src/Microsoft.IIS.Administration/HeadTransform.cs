// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration
{
    using AspNetCore.Http;
    using Core.Http;
    using System;
    using System.IO;
    using System.Threading.Tasks;

    class HeadTransform
    {
        RequestDelegate _next;

        public HeadTransform(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Method.Equals("HEAD", StringComparison.OrdinalIgnoreCase)) {
                context.Request.Method = "GET";

                // Replace the response body to ensure the pipeline can behave normally,
                // but won't actually write to the response body
                using (MemoryStream ms = new MemoryStream()) {

                    var responseStream = context.Response.Body;
                    context.Response.Body = ms;

                    await _next(context);

                    context.Response.ContentLength = ms.Length;
                    context.Request.Method = "HEAD";
                    context.Response.Body = responseStream;
                }
            }
            else {
                await _next(context);
            }
        }
    }
}
