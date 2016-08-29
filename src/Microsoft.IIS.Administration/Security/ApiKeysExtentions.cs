// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Security {
    using AspNetCore.Hosting;
    using Core;
    using Extensions.DependencyInjection;
    using System;
    using System.IO;
    using Core.Security;


    public static class ApiKeysExtentions  {
        public static IServiceCollection AddApiKeyProvider(this IServiceCollection services, Action<ApiKeyOptions> o = null) {
            string configRootPath = services.BuildServiceProvider().GetRequiredService<IHostingEnvironment>().ConfigRootPath();
            var options = new ApiKeyOptions();

            if (o != null) {
                o.Invoke(options);
            }

            services.AddSingleton<IApiKeyProvider>(service => {
                return new ApiKeyProvider(Path.Combine(configRootPath, "api-keys.json"), options);
            });

            return services;
        }
    }
}
