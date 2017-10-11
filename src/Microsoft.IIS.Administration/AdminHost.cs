// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration
{
    using AspNetCore.Builder;
    using AspNetCore.Routing;
    using Core;
    using Extensions.DependencyInjection;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    internal sealed class AdminHost : IAdminHost
    {
        private static AdminHost instance;

        private List<IModule> _modules;
        private IApplicationBuilder _applicationBuilder;
        private IRouteBuilder _routeBuilder;
        private ILookup<string, IModule> _modulesLookup;

        private AdminHost()
        {
            _modules = new List<IModule>();
            _applicationBuilder = null;
            _routeBuilder = null;
        }

        public static AdminHost Instance
        {
            get
            {
                if (instance == null) {
                    instance = new AdminHost();
                }

                return instance;
            }
        }
        public void Add(IModule module)
        {
            _modules.Add(module);
        }

        public void ConfigureModules(IServiceCollection services)
        {
            if (services == null) {
                throw new ArgumentNullException(nameof(services));
            }

            //
            // Configure modules
            foreach (IModule module in _modules) {
                var sa = module as IServiceCollectionAccessor;
                if (sa != null) {
                    sa.Use(services);
                }
            }
        }

        public void StartModules(IRouteBuilder routes, IApplicationBuilder applicationBuilder)
        {
            if (routes == null) {
                throw new ArgumentNullException(nameof(routes));
            }

            if (applicationBuilder == null) {
                throw new ArgumentNullException(nameof(applicationBuilder));
            }

            _routeBuilder = routes;
            _applicationBuilder = applicationBuilder;

            //
            // Initialize modules lookup
            _modulesLookup = _modules.ToLookup((module) => module.GetType().GetTypeInfo().Assembly.FullName);

            //
            // Start modules
            foreach (IModule module in _modules) {
                module.Start();
            }

            //
            // Register edge routes
            MapEdgeWebApiRoutes();
            Release();
        }

        public void StopModules()
        {
            foreach (IModule module in _modules) {
                module.Stop();
            }
        }


        #region IAdminHost method implementations

        public IModule GetModuleByAssemblyName(string assemblyName)
        {
            if(string.IsNullOrEmpty(assemblyName)) {
                throw new ArgumentException(nameof(assemblyName));
            }
            if(!_modulesLookup.Contains(assemblyName)) {
                return null;
            }

            return _modulesLookup[assemblyName].FirstOrDefault();
        }

        public IApplicationBuilder ApplicationBuilder
        {
            get
            {
                if (_applicationBuilder == null) {
                    throw new Exception("Can't register middleware until AdminHost ApplicationBuilder has been initialized.");
                }
                return _applicationBuilder;
            }
        }

        public IRouteBuilder RouteBuilder {
            get {
                if (_routeBuilder == null) {
                    throw new Exception("Can't register routes until AdminHost RouteBuilder has been initialized.");
                }
                return _routeBuilder;
            }
        }

        #endregion
        
        private void MapEdgeWebApiRoutes()
        {

            foreach (var r in _routeBuilder.Routes.ToList())
            {
                Route route = r as Route;
                Guid? resourceId = null;
                object controller = null;

                if (route != null)
                {

                    resourceId = route.Defaults["resourceId"] as Guid?;
                    controller = route.Defaults["controller"];
                }

                if (resourceId != null && controller != null)
                {
                    _routeBuilder.MapWebApiRoute(route.Name + "-edge",
                                route.RouteTemplate + "/{edge}",
                                new {
                                    controller = controller,
                                    action = "Edge",
                                    resourceId = resourceId
                                });
                }
            }
        }

        private void Release()
        {
            _applicationBuilder = null;
            _routeBuilder = null;
        }
    }
}
