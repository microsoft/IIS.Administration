// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Handlers
{
    internal static class HandlersGlobals {

        public const string HandlersSectionName = "system.webServer/handlers";
        public const string ModulesSectionName = "system.webServer/modules";
        public const string ExtensionsSectionName = "system.webServer/security/isapiCgiRestriction";
        public const string FastCgiSectionName = "system.webServer/fastCgi";

        public const string IsapiModuleName = "IsapiModule";
        public const string CgiModuleName = "CgiModule";
        public const string FastCgiModuleName = "FastCgiModule";
    }
}
