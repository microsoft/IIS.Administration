// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration
{
    using AspNetCore.Mvc.Filters;
    using Core;
    using Core.Http;
    using System.Reflection;

    public class ResourceInfoFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context) { }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            string prevtype = string.Empty;
            if (context.HttpContext.Response.Headers.ContainsKey(HeaderNames.ContentType)) {
                prevtype = context.HttpContext.Response.Headers[HeaderNames.ContentType];
            }

            // Don't process responses that have already have resource info or don't have json payloads
            if (prevtype.Contains("vnd") || !prevtype.Contains("json")) {
                return;
            }

            IModule mod = Environment.Host.GetModuleByAssemblyName(context.Controller.GetType().GetTypeInfo().Assembly.FullName);

            string version = mod != null ? mod.Version : string.Empty;

            context.HttpContext.Response.Headers[HeaderNames.ContentType] = ResourceInfoAttribute.ContentType(string.Empty, version, prevtype);
        }
    }
}
