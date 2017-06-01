// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration {
    using System.IO;
    using AspNetCore.Hosting;
    using AspNetCore.Builder;
    using Net.Http.Server;
    using Microsoft.Extensions.Configuration;
    using Microsoft.AspNetCore.Hosting.Server.Features;

    public class Program {
        public static void Main(string[] args) {
            IConfigurationRoot config = Startup.LoadConfig(Path.Combine(Directory.GetCurrentDirectory(), "config"));

            //
            // Host
            using (var host = new WebHostBuilder()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseUrls("https://*:55539") // Config can override it. Use "urls":"https://*:55539"
                .UseConfiguration(config)
                .UseStartup<Startup>()
                .UseWebListener(o => {
                    //
                    // Kernel mode Windows Authentication
                    o.ListenerSettings.Authentication.Schemes = AuthenticationSchemes.Negotiate | AuthenticationSchemes.NTLM;

                    //
                    // Need Anonymos to allow CORs preflight requests
                    // app.UseWindowsAuthentication ensures the request is authenticated to proceed
                    o.ListenerSettings.Authentication.AllowAnonymous = true;
                })
                .Build()
                .UseHttps()) {

                host.Run();
            }
        }
    }
}
