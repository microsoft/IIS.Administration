// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Modules
{

    internal static class ModulesErrors {

        public const string ConfigurationError = "ConfigurationError";
        public const string ManagedModuleAlreadyPresentError = "ModulesManagedModuleAlreadyPresentError";
        public const string GlobalModuleAlreadyPresentError = "ModulesGlobalModuleAlreadyPresentError";
        public const string ModuleNotPresentInGlobalModulesError = "ModulesModuleNotPresentInGlobalModulesError";
        public const string ModuleAlreadyPresentError = "ModulesModuleAlreadyPresentError";
        public const string ModuleNotPresentError = "ModulesModuleNotPresentError";
        public const string GlobalModuleDoesNotExistCannotEditError = "ModulesGlobalModuleDoesNotExistCannotEditError";
        public const string ModuleDoesNotExistCannotEditError = "ModulesModuleDoesNotExistCannotEditError";
        public const string DllPathDoesNotExistError = "ModulesDllPathDoesNotExistError";
        public const string GlobalModuleNotPresentCannotRemoveError = "ModulesGlobalModuleNotPresentError";
        public const string NativeModuleShouldBeADll = "ModulesNativeModuleShouldBeADllError";

    }
}
