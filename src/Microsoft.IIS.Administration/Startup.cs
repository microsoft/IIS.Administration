// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration
{
    using Core;
    using Core.Http;
    using Cors;
    using Extensions.Configuration;
    using Extensions.DependencyInjection;
    using Extensions.DependencyInjection.Extensions;
    using Logging;
    using Security;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using AspNetCore.Antiforgery.Internal;
    using AspNetCore.Builder;
    using AspNetCore.Hosting;
    using AspNetCore.Http;
    using AspNetCore.Routing;
    using AspNetCore.Mvc.ModelBinding;
    using AspNetCore.Mvc;
    using AspNetCore.Mvc.Formatters;
    using AspNetCore.Authorization;

    public class Startup : BaseModule
    {
        public Core.Config.IConfiguration Configuration { get; set; }

        public Startup(IHostingEnvironment env)
        {
            // Set up configuration sources.
            var builder = new ConfigurationBuilder();
            builder.SetBasePath(env.ConfigRootPath());
            builder.AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();
            Configuration = new Core.Config.Configuration(builder.Build());
        }

        // This method gets called by a runtime.
        // Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services)
        {
            var hostEnv = services.BuildServiceProvider().GetRequiredService<IHostingEnvironment>();
            string configRootPath = services.BuildServiceProvider().GetRequiredService<IHostingEnvironment>().ConfigRootPath();

            //
            // Configuration
            services.AddSingleton(typeof(Core.Config.IConfiguration), (s) => Configuration);
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            //
            // Logging
            services.AddApiLogging();

            //
            // Auditing
            services.AddApiAuditing();

            //
            // Load plugins
            ModuleConfig modConfig = new ModuleConfig(Path.Combine(configRootPath, "modules.json"));
            ModuleLoader loader = new ModuleLoader(hostEnv);
            LoadPlugins(loader, modConfig.Modules);

            services.AddOptions();


            //
            // CORS
            services.AddCors();

            //
            // Api Keys
            services.AddApiKeyProvider();

            //
            // Authentication
            services.AddAuthentication();
            services.AddAuthorization(o =>
            {
                o.AddPolicy("AccessToken", p => p.RequireAuthenticatedUser().RequireClaim(Core.Security.ClaimTypes.AccessToken));

                o.AddPolicy("Administrators", p => p.RequireAuthenticatedUser().RequireRole("Administrators"));

                o.AddPolicy("AdministrativeGroup", p => p.RequireAuthenticatedUser().RequireAssertion(authContext => 
                    IsUserInAdministrators(authContext, Configuration)
                ));
            });

            //
            // Antiforgery
            services.TryAddSingleton<IAntiforgeryTokenStore, AntiForgeryTokenStore>();
            services.AddAntiforgery();
            services.AddAntiforgery(o =>
            {
                o.RequireSsl = true;
                o.CookieName = o.FormFieldName = Core.Http.HeaderNames.XSRF_TOKEN;
            });
            
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
                              IHostingEnvironment env)
        {
            //
            // Initialize the Environment
            //
            Core.Environment.Host = AdminHost.Instance;
            Core.Environment.Hal = new HalService();
            AdminHost.Instance.Add(this);

            // Context accessor
            HttpHelper.HttpContextAccessor = app.ApplicationServices.GetRequiredService<IHttpContextAccessor>();

            // Initalize Config
            ConfigurationHelper.Initialize(Path.Combine(env.ConfigRootPath(), "appsettings.json"));
            ConfigurationHelper.Config = Configuration;



            //
            // Error handling
            //
            app.UseMiddleware<ErrorHandler>();

            //
            // Ensure SSL
            //
            app.UseMiddleware<SSLCheck>();

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
            app.UseBearerAuthentication();


            //
            // Authorization
            // 
            app.UseUrlAuthorization(new UrlAuthorizatonOptions
            {
                Path = "/" + Globals.API_PATH,  // /api
                AuthenticationScheme = AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme,
                PolicyName = "AccessToken"
            });

            app.UseUrlAuthorization(new UrlAuthorizatonOptions
            {
                Path = "/" + Globals.SECURITY_PATH, // /security
                AuthenticationScheme = "NTLM",
                PolicyName = "AdministrativeGroup"
            });


            //
            // Disable client cache
            //
            app.Use(async (context, next) =>
            {
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
                AdminHost.Instance.InitiateModules(routes, app);
                InitiateFeatures(routes);

                // Ensure routes meant to be extended do not block child routes
                SortRoutes(routes);
            });
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

        private static bool IsUserInAdministrators(AuthorizationHandlerContext authContext, Core.Config.IConfiguration configuration)
        {
            var winUser = HttpHelper.Current.Authentication.AuthenticateAsync("NTLM").Result;

            foreach (var identifier in configuration.Administrators)
            {

                // Is user in an administrative role
                if (authContext.User.IsInRole(identifier))
                {
                    return true;
                }

                // Is user an administrative user
                foreach (var identity in authContext.User.Identities)
                {
                    if (identity.Name != null && identity.Name.Equals(identifier, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                //
                // Aspnet/Common bug #85 https://github.com/aspnet/Common/issues/85
                // AuthorizationHandlerContext.User.IsInRole does not work for integrated authentication
                // Work around is explicitly calling authenticate for NTLM
                //
                if (winUser != null)
                {
                    if (winUser.IsInRole(identifier))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public override void Start()
        {
            // Nop
        }
    }
}
