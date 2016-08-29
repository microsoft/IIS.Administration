// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Modules
{

    internal static class ModulesGlobals {

        public const string ModulesSectionName = "system.webServer/modules";
        public const string GlobalsModulesSectionName = "system.webServer/globalModules";
    }

    public enum ModuleType {

        Native = 0,

        Managed = 1,
    }
}
