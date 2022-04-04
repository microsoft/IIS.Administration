// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration {
    using AspNetCore.Builder;
    using AspNetCore.Hosting;
    using AspNetCore.Http;
    using AspNetCore.Mvc;
    using AspNetCore.Mvc.Formatters;
    using AspNetCore.Mvc.ModelBinding;
    using AspNetCore.Routing;
    using Core;
    using Core.Http;
    using Cors;
    using Extensions.Configuration;
    using Extensions.DependencyInjection;
    using Extensions.DependencyInjection.Extensions;
    using Extensions.Hosting;
    using Files;
    using Logging;
    using Microsoft.IIS.Administration.Core.Utils;
    using Microsoft.IIS.Administration.Security.Authorization;
    using Security;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.IO;    


    public class Startup : BaseModule {
        private IWebHostEnvironment _hostingEnv;
        private IConfiguration _config;

        public Startup(IWebHostEnvironment env, IConfiguration config) {
            _hostingEnv = env ?? throw new ArgumentNullException(nameof(env));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            Uuid.Key = Guid.Parse(_config.GetValue<string>("host_id")).ToByteArray();
        }


        // This method gets called by a runtime.
        // Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services) {
            //
            // IHttpContextAccessor
            _ = services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            //
            // Logging
            _ = services.AddApiLogging();

            //
            // Auditing
            _ = services.AddApiAuditing();

            //
            // Files
            _ = services.AddFileProvider();

            //
            // Load plugins
            ModuleConfig modConfig = new ModuleConfig(_hostingEnv.GetConfigPath("modules.json"));
            ModuleLoader loader = new ModuleLoader(_hostingEnv);
            LoadPlugins(loader, modConfig.Modules);
            AdminHost.Instance.ConfigureModules(services);

            //
            // CORS
            _ = services.AddCors();

            //
            // Authentication
            _ = services.AddBearerAuthentication();

            //
            // Authorization
            _ = services.AddAuthorizationPolicy();

            services.AddConfigurationWriter(_hostingEnv);

            //
            // Antiforgery
            _ = services.AddAntiforgery(o => 
                {
                    o.Cookie.Name =  HeaderNames.XSRF_TOKEN;
                    o.FormFieldName = HeaderNames.XSRF_TOKEN;
                    o.HeaderName = HeaderNames.XSRF_TOKEN;  // must set header name. It is read from DefaultAntiforgeryTokenStore.cs
                });

            //
            // Caching
            _ = services.AddMemoryCache();
            
            //
            // MVC
            IMvcBuilder builder = services.AddMvc(o => {

                _ = o.Filters.Add(typeof(ActionFoundFilter));

                _ = o.Filters.Add(typeof(ResourceInfoFilter));

                RemoveFilter<UnsupportedContentTypeFilter>(o);

                o.EnableEndpointRouting = false;
            });

            foreach (var asm in loader.GetAllLoadedAssemblies()) {
                _ = builder.AddApplicationPart(asm);
            }

            _ = builder.AddControllersAsServices();
            _ = builder.AddWebApiConventions();
            _ = builder.AddRazorRuntimeCompilation();
        }

        // Configure is called after ConfigureServices is called.
        public void Configure(IApplicationBuilder app,
            IHttpContextAccessor contextAccessor,
            IHostApplicationLifetime applicationLifeTime) {
            //
            // Initialize the Environment
            //
            Core.Environment.Host = AdminHost.Instance;
            Core.Environment.Hal = new HalService();
            AdminHost.Instance.Add(this);

            // Context accessor
            HttpHelper.HttpContextAccessor = contextAccessor;


            //
            // Error handling
            //
            _ = app.UseErrorHandler();


            //
            // Static files
            //
            _ = app.UseStaticFiles();


            //
            // CORS
            //
            _ = app.UseCrossOrigin("/" + Globals.API_PATH);


            //
            // Authentication
            //
            _ = app.UseWindowsAuthentication();


            //
            // Authorization
            // 
            _ = app.UseAuthorizationPolicy();


            //
            // Disable client cache
            //
            _ = app.Use(async (context, next) => {
                context.Response.Headers[Net.Http.Headers.HeaderNames.CacheControl] = "public, max-age=0";
                await next.Invoke();
            });


            //
            // Allow HEAD requests as GET
            _ = app.UseMiddleware<HeadTransform>();


            //
            // Add MVC
            //             
            _ = app.UseMvc(routes => {
                AdminHost.Instance.StartModules(routes, app);
                InitiateFeatures(routes);

                // Ensure routes meant to be extended do not block child routes
                SortRoutes(routes);
            });

            //
            // Register for application shutdown
            _ = applicationLifeTime.ApplicationStopped.Register(() => AdminHost.Instance.StopModules());
        }



        private static void InitiateFeatures(IRouteBuilder routes) {
            //
            // Ping
            _ = routes.MapWebApiRoute("Microsoft.IIS.Administration.Ping",
                                  Globals.PING_PATH,
                                  new { controller = "ping" });

            //
            // Api Root
            _ = routes.MapWebApiRoute(Globals.ApiResource.Guid,
                                  Globals.API_PATH,
                                  new { controller = "ApiRoot" });

            //
            // Access Keys
            _ = routes.MapRoute("Microsoft.IIS.Administration.AccessKeys",
                             $"{Globals.SECURITY_PATH}/tokens/{{action}}",
                             new { controller = "AccessKeys", action = "Index" });

            //
            // MVC
            _ = routes.MapRoute(
                    name: "default",
                    template: "{controller=Explorer}/{action=Index}");
        }

        private void LoadPlugins(ModuleLoader loader, string[] assemblyNames)
        {
            foreach (string assemblyName in assemblyNames) {
                try {
                    _ = loader.LoadModule(assemblyName);
                }
                catch (FileNotFoundException e) {

                    Log.Logger.Fatal($"{e.Message}{System.Environment.NewLine}\tAssembly Name: {assemblyName}{System.Environment.NewLine}\t{e.StackTrace}");
                    throw;
                }
            }
        }

        private static void SortRoutes(IRouteBuilder routes)
        {

            List<KeyValuePair<IRouter, string[]>> routePairs = new List<KeyValuePair<IRouter, string[]>>();
            List<IRouter> nonTemplate = new List<IRouter>();

            foreach (var r in routes.Routes)
            {
                
                var route = r as Route;

                if (route == null)
                {
                    nonTemplate.Add(r);
                }

                else
                {
                    routePairs.Add(new KeyValuePair<IRouter, string[]>(route, route.RouteTemplate.Split('/')));
                }
            }


            routePairs.Sort((kvp1, kvp2) => CompareSegments(kvp1.Value, kvp2.Value));


            routes.Routes.Clear();
            foreach(var r in nonTemplate)
            {
                routes.Routes.Add(r);
            }
            foreach (var r in routePairs)
            {
                routes.Routes.Add(r.Key);
            }
        }

        private static int CompareSegments(string[] segs1, string[] segs2)
        {
            if (segs1.Length != segs2.Length)
            {
                // Sort greatest length first
                return segs2.Length.CompareTo(segs1.Length);
            }

            for (int i = 0; i < segs2.Length; i++)
            {
                var res = string.Compare(segs1[i], segs2[i], StringComparison.OrdinalIgnoreCase);
                if ( res != 0 )
                {
                    return res;
                }
            }

            return 0;
        }

        private static void RemoveFilter<T>(MvcOptions o)
        {

            for (int i = 0; i < o.Filters.Count; i++)
            {
                if (o.Filters[i] is T)
                {
                    o.Filters.RemoveAt(i);
                    break;
                }
            }
        }

        public override void Start()
        {
            // Nop
        }
    }
}
