// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Modules
{
    using Applications;
    using Core.Utils;
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Sites;
    using System.IO;
    using System.Net;
    using Web.Administration;


    [RequireWebServer]
    public class ModulesGeneralController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.ModulesName)]
        public object Get()
        {
            Site site = ApplicationHelper.ResolveSite();
            string path = ApplicationHelper.ResolvePath();

            if (path == null) {
                return NotFound();
            }

            dynamic d = ModuleHelper.ModuleFeatureToJsonModel(site, path);
            return LocationChanged(ModuleHelper.GetModuleGroupLocation(d.id), d);
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.ModulesName)]
        public object Get(string id)
        {
            ModulesId modulesId = new ModulesId(id);

            Site site = modulesId.SiteId == null ? null : SiteHelper.GetSite(modulesId.SiteId.Value);

            if (modulesId.SiteId != null && site == null) {
                Context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }

            return ModuleHelper.ModuleFeatureToJsonModel(site, modulesId.Path);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.ModulesName)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            ModulesId modulesId = new ModulesId(id);

            Site site = modulesId.SiteId == null ? null : SiteHelper.GetSite(modulesId.SiteId.Value);

            if (modulesId.SiteId != null && site == null) {
                Context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }

            ModuleHelper.EnsureValidScope(site, modulesId.Path);

            if (model == null) {
                throw new ApiArgumentException("model");
            }

            // Check for config_scope
            string configPath = model == null ? null : ManagementUnit.ResolveConfigScope(model);
            ModulesSection section = ModuleHelper.GetModulesSection(site, modulesId.Path, configPath);

            try {

                DynamicHelper.If<bool>((object)model.run_all_managed_modules_for_all_requests, v => section.RunAllManagedModulesForAllRequests = v);
                
                if (model.metadata != null) {
                    DynamicHelper.If<OverrideMode>((object)model.metadata.override_mode, v => section.OverrideMode = v);
                }
            }
            catch(FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }

            ManagementUnit.Current.Commit();

            return ModuleHelper.ModuleFeatureToJsonModel(site, modulesId.Path);
        }

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            ModulesId modulesId = new ModulesId(id);

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;

            Site site = (modulesId.SiteId != null) ? SiteHelper.GetSite(modulesId.SiteId.Value) : null;

            if (site == null) {
                return;
            }

            ModuleHelper.EnsureValidScope(site, modulesId.Path);

            ModulesSection section = ModuleHelper.GetModulesSection(site, modulesId.Path, ManagementUnit.ResolveConfigScope());
           
            section.RevertToParent();

            ManagementUnit.Current.Commit();
        }
    }
}
