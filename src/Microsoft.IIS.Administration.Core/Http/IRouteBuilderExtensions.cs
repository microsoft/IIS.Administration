// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core.Http {
    using AspNetCore.Routing;
    using AspNetCore.Builder;
    using System;
    using Utils;

    public static class IRouteBuilderExtensions {

        public static IRouteBuilder MapWebApiRoute(this IRouteBuilder builder, Guid resourceId, string template, object defaults, bool skipEdge = false) {

            //
            // Store the resource guid
            // After Module.Start it will be used to register Edge routes
            dynamic def = (dynamic)defaults.ToExpando();

            if (!skipEdge) {
                def.resourceId = resourceId;
            }

            //
            // Add Route
            return builder.MapWebApiRoute(resourceId.ToString(), template, (object) def);
        }
    }
}
