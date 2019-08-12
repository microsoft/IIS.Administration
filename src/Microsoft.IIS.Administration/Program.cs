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
    using Microsoft.Extensions.Logging.EventLog;
    using Serilog;
    using System;
    using System.Diagnostics;

    public class Program {
        public const string EventSourceName = "Microsoft IIS Administration API";

        public static void Main(string[] args) {
            try
            {
                //
                // Build Config
                var configHelper = new ConfigurationHelper(args);
                IConfiguration config = configHelper.Build();

                //
                // Initialize runAsAService local variable
                string serviceName = config.GetValue<string>("serviceName")?.Trim();
                bool runAsAService = !string.IsNullOrEmpty(serviceName);

                //
                // Host
                using (var host = new WebHostBuilder()
                    .UseContentRoot(configHelper.RootPath)
                    .ConfigureLogging((hostingContext, logging) => {
                        logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));

                    //
                    // Console log is not available in running as a Service
                    if (!runAsAService)
                        {
                            logging.AddConsole();
                        }

                        logging.AddDebug();
                        logging.AddEventLog(new EventLogSettings()
                        {
                            SourceName = EventSourceName
                        });
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
                    .UseHttps())
                {

                    if (runAsAService)
                    {
                        //
                        // Run as a Service
                        Log.Information($"Running as service: {serviceName}");
                        host.RunAsService();
                    }
                    else
                    {
                        //
                        // Run interactive
                        host.Run();
                    }
                }
            }
            catch (Exception ex)
            {
                using (var shutdownLog = new EventLog("Application"))
                {
                    shutdownLog.Source = Program.EventSourceName;
                    shutdownLog.WriteEntry($"Microsoft IIS Administration API has shutdown unexpectively because the error: {ex.ToString()}", EventLogEntryType.Error);
                }
                throw ex;
            }
        }
    }
}
