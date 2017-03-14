// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Modules
{
    using Applications;
    using AspNetCore.Builder;
    using Core;
    using Core.Http;
    using Sites;
    using Web.Administration;



    public class Startup : BaseModule
    {
        public Startup() { }

        public override void Start()
        {
            ConfigureModuleEntries();
            ConfigureModules();
            ConfigureGlobalModules();
        }

        private void ConfigureGlobalModules()
        {
            // Establish MVC route for controller
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.GlobalModulesResource.Guid, $"{Defines.GLOBAL_MODULES_PATH}/{{id?}}", new { controller = "globalmodules" });

            // Self links for resources
            Environment.Hal.ProvideLink(Defines.GlobalModulesResource.Guid, "self", gMod => new { href = ModuleHelper.GetGlobalModuleLocation(gMod.id) });
            
            // Web Server
            Environment.Hal.ProvideLink(WebServer.Defines.Resource.Guid, Defines.GlobalModulesResource.Name, _ => new { href = $"/{Defines.GLOBAL_MODULES_PATH}" });

        }

        private void ConfigureModules()
        {

            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.ModulesResource.Guid, $"{Defines.MODULES_PATH}/{{id?}}", new { controller = "ModulesGeneral" });

            Environment.Hal.ProvideLink(Defines.ModulesResource.Guid, "self", module => new { href = ModuleHelper.GetModuleFeatureLocation(module.id) });

            // Web Server
            Environment.Hal.ProvideLink(WebServer.Defines.Resource.Guid, Defines.ModulesResource.Name, _ => {
                var id = GetModulesId(null, null);
                return new { href = ModuleHelper.GetModuleFeatureLocation(id.Uuid) };
            });

            // Site
            Environment.Hal.ProvideLink(Sites.Defines.Resource.Guid, Defines.ModulesResource.Name, site => {
                var siteId = new SiteId((string)site.id);
                Site s = SiteHelper.GetSite(siteId.Id);
                var id = GetModulesId(s, "/");
                return new { href = ModuleHelper.GetModuleFeatureLocation(id.Uuid) };
            });

            // Application
            Environment.Hal.ProvideLink(Applications.Defines.Resource.Guid, Defines.ModulesResource.Name, app => {
                var appId = new ApplicationId((string)app.id);
                Site s = SiteHelper.GetSite(appId.SiteId);
                var id = GetModulesId(s, appId.Path);
                return new { href = ModuleHelper.GetModuleFeatureLocation(id.Uuid) };
            });
        }

        private void ConfigureModuleEntries()
        {
            // Top level resource routes for plugin
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.ModuleEntriesResource.Guid, $"{ Defines.MODULE_ENTRIES_PATH}/{{id?}}", new { controller = "modules" });

            Environment.Hal.ProvideLink(Defines.ModuleEntriesResource.Guid, "self", entry => new { href = ModuleHelper.GetModuleEntryLocation(entry.id) });

            Environment.Hal.ProvideLink(Defines.ModulesResource.Guid, Defines.ModuleEntriesResource.Name, module => new { href = $"/{Defines.MODULE_ENTRIES_PATH}?{Defines.MODULES_IDENTIFIER}={module.id}" });
        }

        private ModulesId GetModulesId(Site site, string path)
        {
            return new ModulesId(site?.Id, path, ModuleHelper.IsModulesSectionLocal(site, path));
        }
    }
}
