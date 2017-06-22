// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer
{
    using System;
    using System.Linq;
    using Web.Administration;

    public static class FeaturesUtility {

        public static bool GlobalModuleExists(string moduleName)
        {
            ServerManager sm = ManagementUnit.ServerManager;

            var config = sm.GetApplicationHostConfiguration();

            var section = config.GetSection("system.webServer/globalModules");

            var collection = section.GetCollection();

            return collection.Any(configElem => ((string)configElem["name"]).Equals(moduleName, StringComparison.OrdinalIgnoreCase));
        }

        public static bool HasAnyGlobalModule() {
            ServerManager sm = ManagementUnit.ServerManager;

            var config = sm.GetApplicationHostConfiguration();

            var section = config.GetSection("system.webServer/globalModules");

            return section != null && section.GetCollection().Count() > 0;
        }
    }
}
