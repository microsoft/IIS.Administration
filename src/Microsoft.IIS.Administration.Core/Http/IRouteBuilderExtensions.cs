// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core.Http {
    using AspNetCore.Builder;
    using AspNetCore.Routing;
    using AspNetCore.Routing.Constraints;
    using System;
    using System.Collections.Generic;
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
        
        public static IRouteBuilder MapWebApiRoute(
            this IRouteBuilder routeCollectionBuilder,
            string name,
            string template)
        {
            return MapWebApiRoute(routeCollectionBuilder, name, template, defaults: null);
        }

        public static IRouteBuilder MapWebApiRoute(
            this IRouteBuilder routeCollectionBuilder,
            string name,
            string template,
            object defaults)
        {
            return MapWebApiRoute(routeCollectionBuilder, name, template, defaults, constraints: null);
        }

        public static IRouteBuilder MapWebApiRoute(
            this IRouteBuilder routeCollectionBuilder,
            string name,
            string template,
            object defaults,
            object constraints)
        {
            return MapWebApiRoute(routeCollectionBuilder, name, template, defaults, constraints, dataTokens: null);
        }

        /// <summary>
        /// When we use ApiController attribute for Controller classes, MapRoute() does not create a working route
        /// Keep this function here for possible future use.
        /// </summary>
        /// <param name="routeCollectionBuilder"></param>
        /// <param name="name"></param>
        /// <param name="template"></param>
        /// <param name="defaults"></param>
        /// <param name="constraints"></param>
        /// <param name="dataTokens"></param>
        /// <returns></returns>
        public static IRouteBuilder MapWebApiRoute(
            this IRouteBuilder routeCollectionBuilder,
            string name,
            string template,
            object defaults,
            object constraints,
            object dataTokens)
        {
            var mutableDefaults = ObjectToDictionary(defaults);
            mutableDefaults.Add("area", Globals.API_PATH);   // default area name is "api"

            var mutableConstraints = ObjectToDictionary(constraints);
            mutableConstraints.Add("area", new RequiredRouteConstraint());

            return routeCollectionBuilder.MapRoute(name, template, mutableDefaults, mutableConstraints, dataTokens);
        }

        private static IDictionary<string, object> ObjectToDictionary(object value)
        {
            var dictionary = value as IDictionary<string, object>;
            if (dictionary != null)
            {
                return new RouteValueDictionary(dictionary);
            }
            else
            {
                return new RouteValueDictionary(value);
            }
        }
    }
}