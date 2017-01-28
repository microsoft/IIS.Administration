// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using Extensions.Configuration;
    using Extensions.DependencyInjection;

    public static class FileProviderExtensions
    {
        public static IServiceCollection AddFileProvider(this IServiceCollection services)
        {
            services.AddSingleton<IAccessControl>((sp) => new AccessControl(sp.GetRequiredService<IConfiguration>()));

            services.AddSingleton<IFileProvider>((sp) => new FileProvider(sp.GetRequiredService<IAccessControl>()));

            return services;
        }
    }
}
