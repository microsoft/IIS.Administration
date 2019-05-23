// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration {
    using AspNetCore.Builder;
    using AspNetCore.Hosting;
    using Microsoft.AspNetCore.Hosting.WindowsServices;
    using Microsoft.AspNetCore.Server.HttpSys;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Serilog;

    public class Program {
        public static void Main(string[] args) {
            //
            // Build Config
            var configHelper = new ConfigurationHelper(args);
            IConfiguration config = configHelper.Build();

            string serviceName = config.GetValue<string>("serviceName")?.Trim();
            bool runAsAService = !string.IsNullOrEmpty(serviceName);
            //
            // Host
            using (var host = new WebHostBuilder()
                .UseContentRoot(configHelper.RootPath)
                .ConfigureLogging((hostingContext, logging) => {
                    //
                    // Console log is not available in running as a Service
                    if (!runAsAService) {
                        logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                        logging.AddConsole();
                        logging.AddDebug();
                    }
                })
                .UseUrls("https://*:55539") // Config can override it. Use "urls":"https://*:55539"
                .UseConfiguration(config)
                .ConfigureServices(s => s.AddSingleton(config)) // Configuration Service
                .UseStartup<Startup>()
                .UseHttpSys(o => {
                    //
                    // Kernel mode Windows Authentication
                    o.Authentication.Schemes = AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM;

                    //
                    // Need anonymous to allow CORS preflight requests
                    // app.UseWindowsAuthentication ensures (if needed) the request is authenticated to proceed
                    o.Authentication.AllowAnonymous = true;
                })
                .Build()
                .UseHttps()) {

                if (runAsAService) {
                    //
                    // Run as a Service
                    Log.Information($"Running as service: {serviceName}");
                    host.RunAsService();
                }
                else {
                    //
                    // Run interactive
                    host.Run();
                }
            }
        }
    }
}