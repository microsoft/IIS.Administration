// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration
{
    using AspNetCore.Http;
    using AspNetCore.Mvc.Filters;
    using System;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;

    sealed class HeadTransform
    {
        internal const string FOUND_ACTION = "_foundAction";
        private RequestDelegate _next;

        public HeadTransform(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Method.Equals("HEAD", StringComparison.OrdinalIgnoreCase)) {

                // Replace the response body to ensure that it remains empty,
                // if the replacement body is written to, we can use its length for content length
                using (MemoryStream ms = new MemoryStream()) {

                    int status = context.Response.StatusCode;
                    var responseStream = context.Response.Body;
                    context.Response.Body = ms;

                    try {
                        await _next(context);

                        if (context.Response.StatusCode == (int)HttpStatusCode.NotFound && !context.Items.ContainsKey(FOUND_ACTION)) {
                            context.Request.Method = "GET";
                            context.Response.StatusCode = status;

                            await _next(context);
                        }
                    }
                    finally {
                        context.Request.Method = "HEAD";
                        context.Response.Body = responseStream;
                    }

                    context.Response.ContentLength = context.Response.ContentLength > 0 ? context.Response.ContentLength : ms.Length;
                    
                }
            }
            else {
                await _next(context);
            }
        }
    }

    public class ActionFoundFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            context.HttpContext.Items[HeadTransform.FOUND_ACTION] = true;
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}