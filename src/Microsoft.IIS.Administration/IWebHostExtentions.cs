// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration {
    using AspNetCore.Hosting;
    using Microsoft.AspNetCore.Hosting.Server.Features;
    using System;
    using System.Threading;


    public static class IWebHostExtentions  {
        public static IWebHost UseHttps(this IWebHost host) {
            var serverAddresses = host.ServerFeatures.Get<IServerAddressesFeature>();

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
