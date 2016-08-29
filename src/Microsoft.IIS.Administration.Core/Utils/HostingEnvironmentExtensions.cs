// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core
{
    using AspNetCore.Hosting;
    using System.IO;

    public static class HostingEnvironmentExtensions
    {
        private const string DEV_PLUGINS_FOLDER_NAME = "plugins";
        private static readonly string PROD_PLUGINS_FOLDER_NAME = "plugins";

        public static string ConfigRootPath(this IHostingEnvironment env)
        {
            return Path.GetFullPath(Path.Combine(env.WebRootPath, "..", "config"));            
        }

        public static string GetPluginsFolderName(this IHostingEnvironment env)
        {
            return env.IsDevelopment() ? DEV_PLUGINS_FOLDER_NAME : PROD_PLUGINS_FOLDER_NAME;
        }
    }
}
