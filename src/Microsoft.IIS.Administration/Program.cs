// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration {
    using AspNetCore.Builder;
    using AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.IIS.Administration.WindowsService;
    using Net.Http.Server;
    using System;
    using System.IO;


    public class Program {
        public static void Main(string[] args) {

            string rootPath = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != null ?
                              Directory.GetCurrentDirectory() : Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

            //
            // Load Config
            string basePath = Path.Combine(rootPath, "config");
            if (!Directory.Exists(basePath)) {
                throw new FileNotFoundException($"Configuration path \"{basePath}\" doesn't exist. Make sure the working directory is correct.", basePath);
            }

            Startup.Config = new ConfigurationBuilder()
                                .SetBasePath(basePath)
                                .AddJsonFile("appsettings.json")
                                .AddEnvironmentVariables()
                                .AddCommandLine(args)
                                .Build();

            //
            // Host
            using (var host = new WebHostBuilder()
                .UseContentRoot(rootPath)
                .UseUrls("https://*:55539") // Config can override it. Use "urls":"https://*:55539"
                .UseConfiguration(Startup.Config)
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

                var svcHelper = new ServiceHelper(Startup.Config);

                if (svcHelper.IsService) {
                    svcHelper.Run(token => host.Run(token)).Wait();
                }
                else {
                    host.Run();
                }
            }
        }
    }
}