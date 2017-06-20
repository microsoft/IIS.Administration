// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Security {
    using AspNetCore.Hosting;
    using Core;
    using Extensions.DependencyInjection;
    using System;
    using Core.Security;
    using Microsoft.Extensions.Configuration;


    public static class ApiKeysExtentions  {
        public static IServiceCollection AddApiKeyProvider(this IServiceCollection services, Action<ApiKeyOptions> o = null) {
            var options = new ApiKeyOptions();
            if (o != null) {
                o.Invoke(options);
            }

            IHostingEnvironment env = services.BuildServiceProvider().GetRequiredService<IHostingEnvironment>();
            IConfiguration config = services.BuildServiceProvider().GetRequiredService<IConfiguration>();

            var provider = new ApiKeyProvider(options);

            provider.UseStorage(new ApiKeyFileStorage(env.GetConfigPath("api-keys.json")));
            provider.UseStorage(new ApiKeyConfigStorage(config));

            return services.AddSingleton<IApiKeyProvider>(provider);
        }
    }
}
