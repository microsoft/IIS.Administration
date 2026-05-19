// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration {
    using Microsoft.AspNetCore.Hosting.Server;
    using Microsoft.AspNetCore.Hosting.Server.Features;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using System;


    public static class IHostExtensions  {
        public static IHost UseHttps(this IHost host) {
            var server = host.Services.GetRequiredService<IServer>();
            var serverAddresses = server.Features.Get<IServerAddressesFeature>();

            if (serverAddresses != null) {
                foreach (var address in serverAddresses.Addresses) {
                    if (!address.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) {
                        throw new ArgumentException($"{address} - HTTPS is required");
                    }
                }
            }

            return host;
        }
    }
}
