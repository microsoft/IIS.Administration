// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Modules
{
    using System;
    using Web.Administration;
    using System.Collections.Generic;
    using Core.Utils;
    using Core;
    using System.Linq;
    using Sites;
    using System.IO;
    using System.Dynamic;
    using Files;

    public static class ModuleHelper
    {
        private static readonly Fields ModuleRefFields = new Fields("name", "id");
        private static readonly Fields GlobalModuleRefFields = new Fields("name", "id");

        public static GlobalModule CreateGlobalModule(dynamic model)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            string name = DynamicHelper.Value(model.name);
            string image = DynamicHelper.Value(model.image);

            if (string.IsNullOrEmpty(name)) {
                throw new ApiArgumentException("name");
            }
            if (string.IsNullOrEmpty(image)) {
                throw new ApiArgumentException("image");
            }

            AssertCanUseImage(ref image);

            var globalCollection = GetGlobalModulesCollection();

            GlobalModule globalModule = globalCollection.CreateElement();

            globalModule.Name = name;
            globalModule.Image = image;
            globalModule.PreCondition = DynamicHelper.Value(model.precondition) ?? globalModule.PreCondition;

            // This sets the correct bitness precondition 
            string preCondition = globalModule.PreCondition;
            BitnessUtility.AppendBitnessPreCondition(ref preCondition, globalModule.Image);
            globalModule.PreCondition = preCondition;

            return globalModule;
        }

        public static Module CreateManagedModule(dynamic model, ModulesSection section)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            string name = DynamicHelper.Value(model.name);
            string type = DynamicHelper.Value(model.type);

            if (string.IsNullOrEmpty(name)) {
                throw new ApiArgumentException("name");
            }
            if (string.IsNullOrEmpty(type)) {
                throw new ApiArgumentException("type");
            }

            Module module = section.Modules.CreateElement();

            module.Name = name;
            module.Type = type;
            module.PreCondition = DynamicHelper.Value(model.precondition) ?? module.PreCondition;

            return module;
        }

        public static Module AddExistingGlobalModule(GlobalModule globalModule, ModulesSection section) {
            if (globalModule == null) {
                throw new ArgumentNullException("globalModule");
            }

            ModuleCollection collection = section.Modules;
            GlobalModulesCollection globalCollection = GetGlobalModulesCollection();

            GlobalModule element = null;
            if (!IsGlobalModule(globalModule.Name, globalCollection, out element)) {
                throw new Exception(ModulesErrors.ModuleNotPresentInGlobalModulesError);
            }

            Module module = null;
            if (ExistsModule(globalModule.Name, collection, out module)) {
                throw new Exception(ModulesErrors.ModuleAlreadyPresentError);
            }

            try {
                module = collection.Add(globalModule.Name);
            }
            catch(FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }

            if (!String.IsNullOrEmpty(element.PreCondition)) {
                module.PreCondition = element.PreCondition;
            }

            return module;
        }

        public static void AddManagedModule(Module module, ModulesSection section) {
            if (module == null) {
                throw new ArgumentNullException("module");
            }

            ModuleCollection serverCollection = section.Modules;
            GlobalModulesCollection globalCollection = GetGlobalModulesCollection();

            GlobalModule element = null;
            if (IsGlobalModule(module.Name, globalCollection, out element)) {
                throw new ApiArgumentException("Module already exists", "name");
            }

            Module newModule = null;
            if (ExistsModule(module.Name, serverCollection, out newModule)) {
                throw new ApiArgumentException("Module already exists", "name");
            }

            try {
                newModule = serverCollection.Add(module.Name, module.Type);
            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }

            if (!String.IsNullOrEmpty(module.PreCondition)) {
                newModule.PreCondition = module.PreCondition;
            }
        }

        public static void AddGlobalModule(GlobalModule module) {
            if (module == null) {
                throw new ArgumentNullException("nativeModule");
            }
            // NOTE: Can add a new native module only when configuring the server scope
            // and NOT in the location mode

            // Native modules can only be added at the server level
            ModuleCollection serverCollection = GetModulesCollection(null, null);
            GlobalModulesCollection globalCollection = GetGlobalModulesCollection();

            GlobalModule element = null;
            if (IsGlobalModule(module.Name, globalCollection, out element)) {
                throw new AlreadyExistsException("module.name");
            }

            Module action = null;
            if (ExistsModule(module.Name, serverCollection, out action)) {
                throw new AlreadyExistsException("module.name");
            }

            // Add to global modules
            element = globalCollection.Add(module);

            // NOTE: When we add a new native module we do not add it to the server modules
            // list.
        }

        public static void DeleteGlobalModule(GlobalModule module) {
            if (String.IsNullOrEmpty(module.Name)) {
                throw new ArgumentNullException("nativeModule.Name");
            }

            // Native modules can only be configured at the server level
            GlobalModulesCollection globalCollection = GetGlobalModulesCollection();

            GlobalModule element = null;
            if (!IsGlobalModule(module.Name, globalCollection, out element)) {
                return;
            }

            // If the global module is in the enabled modules collection, remove it from there first
            DeleteModule(module.Name, null, null);

            // Remove the global modules from global modules collection
            globalCollection.Remove(element);
        }

        public static void DeleteModule(string moduleName, Site site, string path, string configPath = null) {
            if (moduleName == null) {
                throw new ArgumentNullException("moduleName");
            }

            ModulesSection section = GetModulesSection(site, path, configPath);

            ModuleCollection collection = section.Modules;

            Module module = null;
            if (!ExistsModule(moduleName, collection, out module)) {
                return;
            }

            if (ModuleIsLocked(module) && site != null) {
                // Can't delete the module if it is locked
                // Unless the scope is at server level
                throw new InvalidOperationException("Lock violation");
            }

            try {
                collection.Remove(moduleName);
            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
        }

        public static void UpdateModule(Module module, dynamic model, Site site) {
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (module == null) {
                throw new ArgumentNullException("module");
            }
            if (string.IsNullOrEmpty(module.Name)) {
                throw new ApiArgumentException("module.Name");
            }

            string type = DynamicHelper.Value(model.type) ?? string.Empty;
            string precondition = DynamicHelper.Value(model.precondition) ?? module.PreCondition;

            if (ModuleIsLocked(module) && site != null) {
                // Can't update the module if it is locked
                // Unless the scope is at server level
                throw new InvalidOperationException("Lock violation");
            }

            try {
                // Locked
                bool? locked = DynamicHelper.To<bool>(model.locked);
                if (locked != null) {
                    SetItemLocked(module, locked.Value);
                }

                // Precondition
                if (ConfigurationUtility.ShouldPersist(module.PreCondition, precondition)) {
                    module.PreCondition = precondition;
                }

                // Type
                if (string.IsNullOrEmpty(module.Type) != string.IsNullOrEmpty(type)) {

                    // If the module specified a type, we allow a type name change but not erasure
                    // If the module did not specify a type during creation we don't allow a change
                    throw new ApiArgumentException("type");
                }

                module.Type = type;
            }
            catch (FileLoadException e) {
                throw new LockedException(ModulesGlobals.ModulesSectionName, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
        }

        public static GlobalModule UpdateGlobalModule(GlobalModule globalModule, dynamic model) {
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (globalModule == null) {
                throw new ArgumentNullException("globalModule");
            }

            Configuration config = ManagementUnit.GetConfiguration(null, null);

            GlobalModulesCollection globalCollection = GetGlobalModulesCollection();
            ModuleCollection serverCollection = GetModulesCollection(null, null);

            Module action = null;

            string image = DynamicHelper.Value(model.image);

            if (image != null) {
                AssertCanUseImage(ref image);
                globalModule.Image = image;
            }

            string preCondition = globalModule.PreCondition;
            BitnessUtility.AppendBitnessPreCondition(ref preCondition, image);
            globalModule.PreCondition = preCondition;

            // If the global module is present in the server modules list then we
            // update the precondition of that entry as well.
            if (ExistsModule(globalModule.Name, serverCollection, out action)) {
                if (ConfigurationUtility.ShouldPersist(action.PreCondition, globalModule.PreCondition)) {
                    action.PreCondition = globalModule.PreCondition;
                }
            }

            return globalModule;
        }

        public static bool GlobalModuleExists(string moduleName)
        {
            List<GlobalModule> globalModuleList = GetGlobalModules();
            foreach (GlobalModule globalModule in globalModuleList) {
                if ((globalModule.Name).Equals(moduleName)) {
                    return true;
                }
            }
            return false;
        }

        public static List<GlobalModule> GetGlobalModules()
        {
            // Global modules are only targetted at server scope
            return GetGlobalModulesCollection().ToList();
        }

        public static List<Module> GetModules(Site site, string path, string configPath = null)
        {
            return GetModulesCollection(site, path, configPath).ToList();
        }

        #region Json Model Helpers

        internal static object GlobalModuleToJsonModel(GlobalModule globalModule, Fields fields = null, bool full = true)
        {
            if (globalModule == null) {
                return null;
            }

            if (fields == null)
            {
                fields = Fields.All;
            }

            dynamic obj = new ExpandoObject();

            //
            // name
            if (fields.Exists("name"))
            {
                obj.name = globalModule.Name;
            }

            //
            // id
            obj.id = GlobalModuleId.CreateFromName(globalModule.Name).Uuid;

            //
            // image
            if (fields.Exists("image"))
            {
                obj.image = globalModule.Image;
            }

            //
            // precondition
            if (fields.Exists("precondition"))
            {
                obj.precondition = globalModule.PreCondition;
            }

            return Core.Environment.Hal.Apply(Defines.GlobalModulesResource.Guid, obj, full);
        }

        public static object GlobalModuleToJsonModelRef(GlobalModule globalModule, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return GlobalModuleToJsonModel(globalModule, GlobalModuleRefFields, false);
            }
            else {
                return GlobalModuleToJsonModel(globalModule, fields, false);
            }
        }

        public static object ModuleToJsonModelRef(Module module, Site site, string path, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return ModuleToJsonModel(module, site, path, ModuleRefFields, false);
            }
            else {
                return ModuleToJsonModel(module, site, path, fields, false);
            }
        }

        internal static object ModuleToJsonModel(Module module, Site site, string path, Fields fields = null, bool full = true)
        {
            if (module == null) {
                return null;
            }
            
            if (fields == null)
            {
                fields = Fields.All;
            }

            dynamic obj = new ExpandoObject();

            //
            // name
            if (fields.Exists("name"))
            {
                obj.name = module.Name;
            }

            //
            // id
            obj.id = new EntryId(site?.Id, path, module.Name).Uuid;

            //
            // type
            if (fields.Exists("type"))
            {
                obj.type = module.Type;
            }

            //
            // precondition
            if (fields.Exists("precondition"))
            {
                obj.precondition = module.PreCondition;
            }

            //
            // locked
            if (fields.Exists("locked"))
            {
                obj.locked = ModuleIsLocked(module);
            }

            //
            // modules
            if (fields.Exists("modules"))
            {
                obj.modules = ModuleFeatureToJsonModelRef(site, path);
            }

            return Core.Environment.Hal.Apply(Defines.ModuleEntriesResource.Guid, obj, full);
        }

        internal static object ModuleFeatureToJsonModel(Site site, string path)
        {
            ModulesSection section = GetModulesSection(site, path);

            ModulesId id = new ModulesId(site?.Id, path, section.IsLocallyStored);
            
            var obj = new {
                id = id.Uuid,
                scope = site == null ? string.Empty : site.Name + path,
                metadata = ConfigurationUtility.MetadataToJson(section.IsLocallyStored, section.IsLocked, section.OverrideMode, section.OverrideModeEffective),
                run_all_managed_modules_for_all_requests = section.RunAllManagedModulesForAllRequests,
                website = SiteHelper.ToJsonModelRef(site)
            };

            return Core.Environment.Hal.Apply(Defines.ModulesResource.Guid, obj);
        }

        public static object ModuleFeatureToJsonModelRef(Site site, string path)
        {
            ModulesSection section = GetModulesSection(site, path);

            ModulesId id = new ModulesId(site?.Id, path, section.IsLocallyStored);

            var obj = new {
                id = id.Uuid,
                scope = site == null ? string.Empty : site.Name + path
            };

            return Core.Environment.Hal.Apply(Defines.ModulesResource.Guid, obj, false);
        }

        #endregion

        
        public static ModulesSection GetModulesSection(Site site, string path, string configPath = null)
        {
            return (ModulesSection)ManagementUnit.GetConfigSection(site?.Id,
                                                                           path,
                                                                           ModulesGlobals.ModulesSectionName,
                                                                           typeof(ModulesSection),
                                                                           configPath);
        }

        public static bool IsModulesSectionLocal(Site site, string path)
        {
            return ManagementUnit.IsSectionLocal(site?.Id,
                                                 path,
                                                 ModulesGlobals.ModulesSectionName);
        }

        public static void EnsureValidScope(Site site, string path)
        {
            // scope is valid if it is webserver, a site, or an application

            // Server scope
            if (site == null) {
                return;
            }

            // Site/app scope
            if (site.Applications.Any(app => app.Path.Equals(path, StringComparison.OrdinalIgnoreCase))) {
                return;
            }

            throw new InvalidScopeTypeException(string.Format("{0}{1}", (site == null ? "" : site.Name), path ?? ""));
        }

        public static string GetGlobalModuleLocation(string id) {
            if (id == null) {
                throw new ArgumentNullException(nameof(id));
            }

            return $"/{Defines.GLOBAL_MODULES_PATH}/{id}";
        }

        public static string GetModuleFeatureLocation(string id) {
            if (id == null) {
                throw new ArgumentNullException(nameof(id));
            }

            return $"/{Defines.MODULES_PATH}/{id}";
        }

        public static string GetModuleEntryLocation(string id) {
            if (id == null) {
                throw new ArgumentNullException(nameof(id));
            }

            return $"/{Defines.MODULE_ENTRIES_PATH}/{id}";
        }


        public static string GetModuleGroupLocation(string id)
        {
            return $"/{Defines.MODULES_PATH}/{id}";
        }



        private static void AssertCanUseImage(ref string path)
        {
            path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            var expanded = System.Environment.ExpandEnvironmentVariables(path);

            if (!PathUtil.IsFullPath(expanded)) {
                throw new ApiArgumentException("image");
            }
            if (!File.Exists(expanded)) {
                throw new NotFoundException("image");
            }
        }

        private static bool ExistsModule(string moduleName, ModuleCollection collection,
                                  out Module action)
        {
            action = collection[moduleName];
            if (action != null) {
                return true;
            }

            return false;
        }

        private static ConfigurationSection GetSection(Configuration config, string sectionName, Type sectionType) {
            if (config == null) {
                throw new Exception(ModulesErrors.ConfigurationError);
            }

            ConfigurationSection section = config.GetSection(sectionName, sectionType);
            if (section == null) {
                throw new Exception(ModulesErrors.ConfigurationError);
            }

            return section;
        }

        private static GlobalModulesCollection GetGlobalModulesCollection()
        {
            Configuration config = ManagementUnit.GetConfiguration(null, null);

            GlobalModulesSection section =
                (GlobalModulesSection)GetSection(config, ModulesGlobals.GlobalsModulesSectionName, typeof(GlobalModulesSection));

            GlobalModulesCollection collection = section.GlobalModules;
            if (collection == null) {
                throw new Exception(ModulesErrors.ConfigurationError);
            }

            return collection;
        }

        private static ModuleCollection GetModulesCollection(Site site, string path, string configPath = null) {
            ModulesSection section = GetModulesSection(site, path, configPath);

            ModuleCollection collection = section.Modules;
            if (collection == null) {
                throw new Exception(ModulesErrors.ConfigurationError);
            }

            return collection;
        }

        private static bool IsGlobalModule(string moduleName, GlobalModulesCollection collection,
                                    out GlobalModule element) {
            element = null;
            foreach (GlobalModule e in collection) {
                if (String.Equals(e.Name, moduleName, StringComparison.Ordinal)) {
                    element = e;
                    return true;
                }
            }

            return false;
        }

        private static bool ModuleIsLocked(Module module)
        {
            object o = module.GetMetadata("lockItem");
            if (o != null) {
                return (bool)o;
            }
            return false;
        }

        private static void SetItemLocked(Module module, bool locked)
        {
            if (module == null) {
                throw new ArgumentNullException("module");
            }

            module.SetMetadata("lockItem", locked);
        }
    }
}
