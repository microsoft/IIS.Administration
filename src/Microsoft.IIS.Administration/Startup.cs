// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration {
    using AspNetCore.Antiforgery.Internal;
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
        private IHostingEnvironment _hostingEnv;
        private IConfiguration _config;

        public Startup(IHostingEnvironment env, IConfiguration config) {
            _hostingEnv = env ?? throw new ArgumentNullException(nameof(env));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            Uuid.Key = Guid.Parse(_config.GetValue<string>("host_id")).ToByteArray();
        }


        // This method gets called by a runtime.
        // Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services) {
            //
            // IHttpContextAccessor
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            //
            // Logging
            services.AddApiLogging();

            //
            // Auditing
            services.AddApiAuditing();

            //
            // Files
            services.AddFileProvider();

            //
            // Load plugins
            ModuleConfig modConfig = new ModuleConfig(_hostingEnv.GetConfigPath("modules.json"));
            ModuleLoader loader = new ModuleLoader(_hostingEnv);
            LoadPlugins(loader, modConfig.Modules);
            AdminHost.Instance.ConfigureModules(services);

            //
            // CORS
            services.AddCors();

            //
            // Authentication
            services.AddBearerAuthentication();

            //
            // Authorization
            services.AddAuthorizationPolicy();

            services.AddConfigurationWriter(_hostingEnv);

            //
            // Antiforgery
            services.TryAddSingleton<IAntiforgeryTokenStore, AntiForgeryTokenStore>();
            services.AddAntiforgery(o => {
                o.RequireSsl = true;
                o.CookieName = o.FormFieldName = HeaderNames.XSRF_TOKEN;
            });

            //
            // Caching
            services.AddMemoryCache();
            
            //
            // MVC
            IMvcBuilder builder = services.AddMvc(o => {

                // Replace default json output formatter
                o.OutputFormatters.RemoveType<AspNetCore.Mvc.Formatters.JsonOutputFormatter>();

                var settings = JsonSerializerSettingsProvider.CreateSerializerSettings();
                o.OutputFormatters.Add(new JsonOutputFormatter(settings, System.Buffers.ArrayPool<char>.Shared));

                // TODO
                // Workaround filter to fix Object Results returned from controllers
                // Remove when https://github.com/aspnet/Mvc/issues/4960 is resolved
                o.Filters.Add(typeof(Fix4960ActionFilter));

                o.Filters.Add(typeof(ActionFoundFilter));

                o.Filters.Add(typeof(ResourceInfoFilter));

                RemoveFilter<UnsupportedContentTypeFilter>(o);
            });

            foreach (var asm in loader.GetAllLoadedAssemblies()) {
                builder.AddApplicationPart(asm);
            }

            builder.AddControllersAsServices();
            builder.AddWebApiConventions();
        }

        // Configure is called after ConfigureServices is called.
        public void Configure(IApplicationBuilder app,
            IHttpContextAccessor contextAccessor,
            IApplicationLifetime applicationLifeTime) {
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
            app.UseErrorHandler();


            //
            // Static files
            //
            app.UseStaticFiles();


            //
            // CORS
            //
            app.UseCrossOrigin("/" + Globals.API_PATH);


            //
            // Authentication
            //
            app.UseWindowsAuthentication();


            //
            // Authorization
            // 
            app.UseAuthorizationPolicy();


            //
            // Disable client cache
            //
            app.Use(async (context, next) => {
                context.Response.Headers[Net.Http.Headers.HeaderNames.CacheControl] = "public, max-age=0";
                await next.Invoke();
            });


            //
            // Allow HEAD requests as GET
            app.UseMiddleware<HeadTransform>();


            //
            // Add MVC
            // 
            app.UseMvc(routes => {
                AdminHost.Instance.StartModules(routes, app);
                InitiateFeatures(routes);

                // Ensure routes meant to be extended do not block child routes
                SortRoutes(routes);
            });

            //
            // Register for application shutdown
            applicationLifeTime.ApplicationStopped.Register(() => AdminHost.Instance.StopModules());
        }



        private static void InitiateFeatures(IRouteBuilder routes) {
            //
            // Ping
            routes.MapWebApiRoute("Microsoft.IIS.Administration.Ping",
                                  Globals.PING_PATH,
                                  new { controller = "ping" });

            //
            // Api Root
            routes.MapWebApiRoute(Globals.ApiResource.Guid,
                                  Globals.API_PATH,
                                  new { controller = "ApiRoot" });

            //
            // Access Keys
            routes.MapRoute("Microsoft.IIS.Administration.AccessKeys",
                             $"{Globals.SECURITY_PATH}/tokens/{{action}}",
                             new { controller = "AccessKeys", action = "Index" });

            //
            // MVC
            routes.MapRoute(
                    name: "default",
                    template: "{controller=Explorer}/{action=Index}");
        }

        private void LoadPlugins(ModuleLoader loader, string[] assemblyNames)
        {
            foreach (string assemblyName in assemblyNames) {
                try {
                    loader.LoadModule(assemblyName);
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


            routePairs.Sort((kvp1, kvp2) =>
            {
                return CompareSegments(kvp1.Value, kvp2.Value);
            });


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
