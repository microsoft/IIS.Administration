// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Modules
{
    using Applications;
    using AspNetCore.Mvc;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using Web.Administration;
    using Core.Http;
    using Core.Utils;
    using Core;


    [RequireWebServer]
    public class GlobalModulesController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.GlobalModulesName)]
        public object Get()
        {
            // Check if the scope of the request is for site 
            Site site = ApplicationHelper.ResolveSite();

            // Cannot target the global modules from site scope or deeper
            if(site != null) {
                return NotFound();
            }

            List<GlobalModule> modules = ModuleHelper.GetGlobalModules();

            this.Context.Response.SetItemsCount(modules.Count());

            Fields fields = Context.Request.GetFields();

            return new {
                global_modules = modules.Select(globalModule => ModuleHelper.GlobalModuleToJsonModelRef(globalModule, fields))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.GlobalModuleName)]
        public object Get(string id)
        {
            GlobalModuleId moduleId = GlobalModuleId.CreateFromUuid(id);

            GlobalModule module = ModuleHelper.GetGlobalModules().FirstOrDefault(m => m.Name.Equals(moduleId.Name));

            if(module == null) {
                return NotFound();
            }

            return ModuleHelper.GlobalModuleToJsonModel(module);
        }

        [HttpPost]
        [Audit]
        [ResourceInfo(Name = Defines.GlobalModuleName)]
        public object Post([FromBody] dynamic model)
        {
            GlobalModule module = null;

            // Create a global module
            module = ModuleHelper.CreateGlobalModule(model);

            // Save it
            ModuleHelper.AddGlobalModule(module);
            ManagementUnit.Current.Commit();

            //
            // Create response
            dynamic gm = ModuleHelper.GlobalModuleToJsonModel(module);

            return Created(ModuleHelper.GetGlobalModuleLocation(gm.id), gm);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.GlobalModuleName)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            GlobalModuleId moduleId = GlobalModuleId.CreateFromUuid(id);

            GlobalModule module = ModuleHelper.GetGlobalModules().FirstOrDefault(m => m.Name.Equals(moduleId.Name));

            if (module == null) {
                return NotFound();
            }

            module = ModuleHelper.UpdateGlobalModule(module, model);

            ManagementUnit.Current.Commit();

            return ModuleHelper.GlobalModuleToJsonModel(module);
        }

        [HttpDelete]
        [Audit]
        public void Delete(string id, [FromBody] dynamic model)
        {
            GlobalModuleId moduleId = GlobalModuleId.CreateFromUuid(id);

            GlobalModule module = ModuleHelper.GetGlobalModules().FirstOrDefault(m => m.Name.Equals(moduleId.Name));

            if (module != null) {

                // Delete target global module
                ModuleHelper.DeleteGlobalModule(module);

                // Save changes
                ManagementUnit.Current.Commit();
            }

            // Success
            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }
    }
}
