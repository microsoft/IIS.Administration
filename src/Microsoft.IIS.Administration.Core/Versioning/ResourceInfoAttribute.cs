
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core
{
    using AspNetCore.Mvc.Filters;
    using Http;
    using System;
    using System.Reflection;
    using System.Text;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ResourceInfoAttribute : ActionFilterAttribute
    {
        public string Name { get; set; }
        public string Version { get; set; }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            string prevtype = "";
            if (context.HttpContext.Response.Headers.ContainsKey(HeaderNames.ContentType)) {
                prevtype = context.HttpContext.Response.Headers[HeaderNames.ContentType];
            }

            if (this.Version == null) {
                IModule mod = Environment.Host.GetModuleByAssemblyName(context.Controller.GetType().GetTypeInfo().Assembly.FullName);

                this.Version = mod != null ? mod.Version : this.Version;
            }

            context.HttpContext.Response.Headers[HeaderNames.ContentType] = ContentType(this.Name, this.Version, prevtype);
        }

        public static string ContentType(string name, string version, string previous = null)
        {
            StringBuilder type = new StringBuilder();
            type.Append("application/vnd.");
            type.Append(name);
            type.Append(".");
            type.Append(version);

            if (previous == null || !previous.Contains("+json")) {
                type.Append("+json");
            }
            else {
                type.Append(".");
                if (previous.Contains("application/hal"))
                    previous = previous.Remove(0, "application/".Length);
                type.Append(previous);
            }

            return type.ToString();
        }
    }
}
