// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Modules
{
    using AspNetCore.Mvc;
    using Core;
    using Core.Utils;
    using Newtonsoft.Json.Linq;
    using Sites;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using Web.Administration;
    using Core.Http;


    [RequireWebServer]
    public class ModulesController : ApiBaseController {

        [HttpGet]
        [ResourceInfo(Name = Defines.ModuleEntriesName)]
        public object Get() {
            string modulesUuid = Context.Request.Query[Defines.MODULES_IDENTIFIER];

            if (string.IsNullOrEmpty(modulesUuid)) {
                return new StatusCodeResult((int)HttpStatusCode.NotFound);
            }

            ModulesId id = new ModulesId(modulesUuid);

            Site site = id.SiteId == null ? null : SiteHelper.GetSite(id.SiteId.Value);

            List<Module> modules = ModuleHelper.GetModules(site, id.Path);

            // Set HTTP header for total count
            this.Context.Response.SetItemsCount(modules.Count());

            Fields fields = Context.Request.GetFields();

            return new {
                entries = modules.Select(module => ModuleHelper.ModuleToJsonModelRef(module, site, id.Path, fields))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.ModuleEntryName)]
        public object Get(string id) {
            EntryId entryId = new EntryId(id);

            Site site = entryId.SiteId == null ? null : SiteHelper.GetSite(entryId.SiteId.Value);

            Module module = null;

            // Get the enabled modules
            List<Module> modules = ModuleHelper.GetModules(site, entryId.Path);

            module = modules.FirstOrDefault(m => m.Name.Equals(entryId.Name));

            // Module id did not specify an enabled module
            if (module == null) {
                return new StatusCodeResult((int)HttpStatusCode.NotFound);
            }

            return ModuleHelper.ModuleToJsonModel(module, site, entryId.Path);
        }

        [HttpPost]
        [Audit]
        [ResourceInfo(Name = Defines.ModuleEntryName)]
        public object Post([FromBody] dynamic model) {
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (model.modules == null || !(model.modules is JObject)) {
                throw new ApiArgumentException("modules");
            }

            string modulesUuid = DynamicHelper.Value(model.modules.id);
            if (modulesUuid == null) {
                throw new ApiArgumentException("modules.id");
            }

            // Get the feature id
            Module module = null;
            ModulesId modulesId = new ModulesId(modulesUuid);
            Site site = modulesId.SiteId == null ? null : SiteHelper.GetSite(modulesId.SiteId.Value);

            if (modulesId.SiteId != null && site == null) {
                Context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }

            ModuleHelper.EnsureValidScope(site, modulesId.Path);

            string configPath = ManagementUnit.ResolveConfigScope(model);
            ModulesSection section = ModuleHelper.GetModulesSection(site, modulesId.Path, configPath);

            // The post could either be creating a Managed module, or adding an existing
            // global module to the modules list.
            // This information is taken from the model's type value
            string type = DynamicHelper.Value(model.type);

            if (string.IsNullOrEmpty(type)) {

                // The module being added is a global/native module

                string name = DynamicHelper.Value(model.name);

                if (string.IsNullOrEmpty(name)) {
                    throw new ApiArgumentException("name");
                }

                GlobalModule existingGlobalModule = ModuleHelper.GetGlobalModules().FirstOrDefault(m => m.Name.Equals(name));

                // Adding a global module to the modules list means it must already exist in global modules
                if (existingGlobalModule == null) {
                    throw new NotFoundException("name");
                }

                // Add the existing global module
                module = ModuleHelper.AddExistingGlobalModule(existingGlobalModule, section);
                ManagementUnit.Current.Commit();
            }
            else {
                // Module being added to enabled modules is a managed module

                // Create module from model
                module = ModuleHelper.CreateManagedModule(model, section);

                // Save it
                ModuleHelper.AddManagedModule(module, section);
                ManagementUnit.Current.Commit();
            }


            //
            // Create response
            dynamic moduleEntry = ModuleHelper.ModuleToJsonModel(module, site, modulesId.Path);

            return Created(ModuleHelper.GetModuleEntryLocation(moduleEntry.id), moduleEntry);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.ModuleEntryName)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            EntryId entryId = new EntryId(id);

            Site site = entryId.SiteId == null ? null : SiteHelper.GetSite(entryId.SiteId.Value);

            if (entryId.SiteId != null && site == null) {
                Context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }

            ModuleHelper.EnsureValidScope(site, entryId.Path);

            // Get the enabled modules
            string configPath = ManagementUnit.ResolveConfigScope(model);
            List<Module> modules = ModuleHelper.GetModules(site, entryId.Path, configPath);

            Module module = modules.FirstOrDefault(m => m.Name.Equals(entryId.Name));

            if (module == null) {
                return NotFound();
            }

            ModuleHelper.UpdateModule(module, model, site);

            ManagementUnit.Current.Commit();

            //
            // Create response
            dynamic entry = (dynamic) ModuleHelper.ModuleToJsonModel(module, site, entryId.Path);

            if (entry.id != id) {
                return LocationChanged(ModuleHelper.GetModuleEntryLocation(entry.id), entry);
            }

            return entry;
        }

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            EntryId entryId = new EntryId(id);

            Site site = entryId.SiteId == null ? null : SiteHelper.GetSite(entryId.SiteId.Value);


            if (entryId.SiteId != null && site == null) {
                Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                return;
            }

            ModuleHelper.EnsureValidScope(site, entryId.Path);

            Module module = null;

            List<Module> modules = ModuleHelper.GetModules(site, entryId.Path);

            module = modules.FirstOrDefault(m => m.Name.Equals(entryId.Name));

            if (module != null) {

                ModuleHelper.DeleteModule(module.Name, site, entryId.Path, ManagementUnit.ResolveConfigScope());
                ManagementUnit.Current.Commit();
            }

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }
    }
}
