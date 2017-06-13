// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core.Http {
    using System;
    using System.Web.Http;
    using AspNetCore.Authorization;
    using AspNetCore.Http;
    using AspNetCore.Mvc;
    using Utils;


    [Authorize]
    public abstract class ApiEdgeController : ApiController {
        [HttpGet]
        public object Edge(string edge, string id = "") {
            edge = edge.ToLower();

            if (edge == "self") {
                return NotFound();
            }

            Guid guid = (Guid) ActionContext.RouteData.Values["resourceId"];

            object obj = new {
                id = !string.IsNullOrEmpty(id) ? id : GetId()
            };

            var links = Core.Environment.Hal.Get(guid, obj.ToExpando());

            string redirectUrl = null;
            object link;

            if (links.TryGetValue(edge, out link)) {
                redirectUrl = ((dynamic)link.ToExpando()).href;
            }

            if (redirectUrl == null || Context.Request.Path.StartsWithSegments(redirectUrl)) {
                return NotFound();
            }

            //
            // Append QueryString
            var uri = new Uri(redirectUrl, UriKind.RelativeOrAbsolute);

            // Make absolute Uri
            if (!uri.IsAbsoluteUri) {
                var baseUrl = new Uri(Request.RequestUri.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped), UriKind.Absolute);
                uri = new Uri(baseUrl, redirectUrl);
            }

            var qs = QueryString.FromUriComponent(uri) + Context.Request.QueryString;

            string location = uri.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.Unescaped) + qs.ToUriComponent();

            return Redirect(location);
        }

        protected virtual string GetId() {
            return string.Empty;
        }        
    }
}
