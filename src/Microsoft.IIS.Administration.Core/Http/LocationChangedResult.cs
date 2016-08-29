// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core.Http {
    using System;
    using AspNetCore.Mvc;
    

    public class LocationChangedResult : ObjectResult {
        public LocationChangedResult(string location, object value)
            : base(value)
        {
            if (location == null) {
                throw new ArgumentNullException(nameof(location));
            }

            //
            // The 209 (Contents of Related) status code indicates that the server is redirecting the user agent to
            // a different resource, as indicated by a URI in the Location header field, that is intended to provide
            // an indirect response to the original request. 
            // http://www.w3.org/2014/02/2xx/draft-prudhommeaux-http-status-209
            //

            Location = location;
            StatusCode = 209; // HTTP 209 Contents of Related
        }

        public string Location { get; set; }

        public override void OnFormatting(ActionContext context) {
            if (context == null) {
                throw new ArgumentNullException(nameof(context));
            }

            base.OnFormatting(context);

            if (Location != null) {
                context.HttpContext.Response.Headers[Net.Http.Headers.HeaderNames.Location] = Location;
            }
        }
    }
}
