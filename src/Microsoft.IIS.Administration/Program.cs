// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration {
    using AspNetCore.Builder;
    using AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.IIS.Administration.WindowsService;
    using Net.Http.Server;


    public class Program {
        public static void Main(string[] args) {
            //
            // Build Config
            var configHelper = new ConfigurationHelper(args);
            IConfiguration config = configHelper.Build();

            //
            // Host
            using (var host = new WebHostBuilder()
                .UseContentRoot(configHelper.RootPath)
                .UseUrls("https://*:55539") // Config can override it. Use "urls":"https://*:55539"
                .UseConfiguration(config)
                .ConfigureServices(s => s.AddSingleton(config)) // Configuration Service
                .UseStartup<Startup>()
                .UseWebListener(o => {
                    //
                    // Kernel mode Windows Authentication
                    o.ListenerSettings.Authentication.Schemes = AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM;

                    //
                    // Need Anonymos to allow CORs preflight requests
                    // app.UseWindowsAuthentication ensures (if needed) the request is authenticated to proceed
                    o.ListenerSettings.Authentication.AllowAnonymous = true;
                })
                .Build()
                .UseHttps()) {

                string serviceName = config.GetValue<string>("serviceName")?.Trim();

                if (!string.IsNullOrEmpty(serviceName)) {
                    //
                    // Run as a Service
                    new ServiceHelper(serviceName).Run(token => host.Run(token))
                                                  .Wait();
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