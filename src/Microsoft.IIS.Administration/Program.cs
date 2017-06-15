// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration {
    using AspNetCore.Builder;
    using AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.IIS.Administration.WindowsService;
    using Net.Http.Server;


    public class Program {
        public static void Main(string[] args) {

            //
            // Build Config
            var config = new ConfigurationHelper();

            Startup.Config = config.Build(args);

            //
            // Host
            using (var host = new WebHostBuilder()
                .UseContentRoot(config.RootPath)
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

                var svcHelper = new ServiceHelper((IConfiguration)host.Services.GetService(typeof(IConfiguration)));

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