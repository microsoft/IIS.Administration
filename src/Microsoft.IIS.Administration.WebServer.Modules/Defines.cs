// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Modules {
    using Core;
    using System;

    public class Defines
    {
        private const string GLOBAL_MODULES_ENDPOINT = "global-modules";
        private const string MODULES_ENDPOINT = "http-modules";
        private const string MODULE_ENTRIES_ENDPOINT = "entries";

        // Global modules
        public const string GlobalModulesName = "Microsoft.WebServer.GlobalModules";
        public const string GlobalModuleName = "Microsoft.WebServer.GlobalModule";
        public static readonly string GLOBAL_MODULES_PATH = $"{WebServer.Defines.PATH}/{GLOBAL_MODULES_ENDPOINT}";
        internal static ResDef GlobalModulesResource = new ResDef("global_modules", new Guid("9C52EBA8-7242-4E99-9953-8B3A25F5D3F1"), GLOBAL_MODULES_ENDPOINT);
        public const string GLOBAL_MODULES_IDENTIFIER = "global_module.id";

        // Module Groups
        public const string ModulesName = "Microsoft.WebServer.Modules";
        public static readonly string MODULES_PATH = $"{WebServer.Defines.PATH}/{MODULES_ENDPOINT}";
        internal static ResDef ModulesResource = new ResDef("modules", new Guid("7F2A09AE-5031-415C-9CC7-B1266065D3B4"), MODULES_ENDPOINT);
        public const string MODULES_IDENTIFIER = "modules.id";

        // Modules
        public const string ModuleEntriesName = "Microsoft.WebServer.Modules.Entries";
        public const string ModuleEntryName = "Microsoft.WebServer.Modules.Entry";
        public static readonly string MODULE_ENTRIES_PATH = $"{MODULES_PATH}/{MODULE_ENTRIES_ENDPOINT}";
        internal static ResDef ModuleEntriesResource = new ResDef("entries", new Guid("CC95AA03-6423-43BC-95F0-3A5919BE0AF9"), MODULE_ENTRIES_ENDPOINT);
        public const string MODULE_ENTRIES_IDENTIFIER = "entry.id";
    }
}
