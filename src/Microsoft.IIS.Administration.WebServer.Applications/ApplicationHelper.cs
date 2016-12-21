// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Applications
{
    using AppPools;
    using Core;
    using Core.Utils;
    using Newtonsoft.Json.Linq;
    using Sites;
    using System;
    using System.IO;
    using Web.Administration;
    using Core.Http;
    using System.Collections.Generic;
    using System.Dynamic;
    using Files;

    public static class ApplicationHelper {

        private static readonly Fields RefFields = new Fields("location", "id", "path");

        public static Application CreateApplication(dynamic model, Site site)
        {
            // Ensure necessary information provided
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (String.IsNullOrEmpty(DynamicHelper.Value(model.path))) {
                throw new ApiArgumentException("path");
            }
            if (string.IsNullOrEmpty(DynamicHelper.Value(model.physical_path))) {
                throw new ApiArgumentException("physical_path");
            }
            if (site == null) {
                throw new ArgumentException("site");
            }

            Application app = site.Applications.CreateElement();

            // Initialize root virtual directory
            app.VirtualDirectories.Add("/", string.Empty);
            
            // Initialize new application settings
            SetToDefaults(app);

            // Set application settings to those provided
            SetApplication(app, model);
            
            return app;
        }

        public static Application GetApplication(string path, Site site)
        {
            if(site == null || string.IsNullOrEmpty(path)) {
                return null;
            }
            return site.Applications[path];
        }

        public static IEnumerable<ApplicationInfo> GetApplications(ApplicationPool pool) {
            if (pool == null) {
                throw new ArgumentNullException(nameof(pool));
            }

            var apps = new List<ApplicationInfo>();

            var sm = ManagementUnit.ServerManager;

            foreach (var site in sm.Sites) {
                foreach (var app in site.Applications) {
                    if (app.ApplicationPoolName.Equals(pool.Name, StringComparison.OrdinalIgnoreCase)) {
                        apps.Add(new ApplicationInfo {
                            Application = app,
                            Site = site
                        });
                    }
                }
            }

            return apps;
        }

        public static void UpdateApplication(Application app, Site site, dynamic model)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (app == null) {
                throw new ArgumentNullException("app");
            }
            if (site == null) {
                throw new ArgumentNullException("site");
            }

            // Update state of site to those specified in the model
            SetApplication(app, model);
        }

        public static void DeleteApplication(Application app, Site site)
        {
            if(app == null) {
                throw new ArgumentNullException("app");
            }
            if (site == null) {
                throw new ArgumentNullException("site");
            }

            // Make sure the application with given path exists in the site
            if (site.Applications[app.Path] != null) {
                site.Applications.Remove(site.Applications[app.Path]);
            }
        }

        internal static object ToJsonModel(Application app, Site site, Fields fields = null, bool full = true) {
            if (app == null) {
                return null;
            }

            if(site == null) {
                throw new ArgumentNullException("site");
            }

            if (fields == null) {
                fields = Fields.All;
            }

            dynamic obj = new ExpandoObject();

            // location
            if (fields.Exists("location")) {
                obj.location = $"{site.Name}{app.Path}";
            }

            //
            // path
            if (fields.Exists("path")) {
                obj.path = app.Path;
            }

            //
            // id
            obj.id = new ApplicationId(app.Path, site.Id).Uuid;

            //
            // physical_path
            if (fields.Exists("physical_path")) {
                // Obtain path from the root vdir
                VirtualDirectory vdir = app.VirtualDirectories["/"];

                if (vdir != null) {
                    obj.physical_path = vdir.PhysicalPath;
                }
            }

            //
            // enabled_protocols
            if (fields.Exists("enabled_protocols")) {
                obj.enabled_protocols = app.EnabledProtocols;
            }

            // website
            if (fields.Exists("website")) {
                obj.website = SiteHelper.ToJsonModelRef(site, fields.Filter("website"));
            }

            //
            // application_pool
            if (fields.Exists("application_pool")) {
                ApplicationPool pool = AppPoolHelper.GetAppPool(app.ApplicationPoolName);
                obj.application_pool = pool != null ? AppPoolHelper.ToJsonModelRef(pool, fields.Filter("application_pool")) : null;
            }

            return Core.Environment.Hal.Apply(Defines.Resource.Guid, obj, full);
        }

        public static object ToJsonModelRef(Application app, Site site, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return ToJsonModel(app, site, RefFields, false);
            }
            else {
                return ToJsonModel(app, site, fields, false);
            }
        }

        public static Site ResolveSite(dynamic model = null)
        {
            string appUuid = null;

            if (model != null && model.webapp != null) {
                if(!(model.webapp is JObject)) {
                    throw new ApiArgumentException("webapp");
                }

                appUuid = DynamicHelper.Value(model.webapp.id);
            }

            var context = HttpHelper.Current;

            if (appUuid == null) {
                appUuid = context.Request.Query[Defines.IDENTIFIER];
            }

            if(appUuid != null) {
                var site = SiteHelper.GetSite(new ApplicationId(appUuid).SiteId);

                if (site == null) {
                    throw new NotFoundException("site");
                }

                return site;
            }

            return SiteHelper.ResolveSite(model);
        }

        public static string GetLocation(string id) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentNullException(nameof(id));
            }

            return $"/{Defines.PATH}/{id}";
        }


        public static string ResolvePath(dynamic model = null)
        {
            string appUuid = null;

            if (model != null && model.webapp != null) {
                if (!(model.webapp is JObject)) {
                    throw new ApiArgumentException("webapp");
                }

                appUuid = DynamicHelper.Value(model.webapp.id);
            }

            var context = HttpHelper.Current;

            if (appUuid == null) {
                appUuid = context.Request.Query[Defines.IDENTIFIER];
            }

            if (appUuid != null) {
                ApplicationId id = new ApplicationId(appUuid);

                return id.Path;
            }

            return SiteHelper.ResolvePath(model);
        }
        

        private static void SetToDefaults(Application app)
        {
            ApplicationDefaults defaults = ManagementUnit.ServerManager.ApplicationDefaults;

            app.ApplicationPoolName = defaults.ApplicationPoolName;
            app.EnabledProtocols = defaults.EnabledProtocols;
        }

        private static void SetApplication(Application app, dynamic model)
        {
            string path = DynamicHelper.Value(model.path);
            if(!string.IsNullOrEmpty(path)) {

                // Make sure path starts with '/'
                if(path[0] != '/') {
                    path = '/' + path;
                }

                app.Path = path;
            }

            DynamicHelper.If((object)model.enabled_protocols, v => app.EnabledProtocols = v);

            string physicalPath = DynamicHelper.Value(model.physical_path);

            if (physicalPath != null) {

                physicalPath = physicalPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                var expanded = System.Environment.ExpandEnvironmentVariables(physicalPath);

                if (!PathUtil.IsFullPath(expanded)) {
                    throw new ApiArgumentException("physical_path");
                }
                if (!FileProvider.Default.IsAccessAllowed(expanded, FileAccess.Read)) {
                    throw new ForbiddenArgumentException("physical_path", physicalPath);
                }
                if (!Directory.Exists(expanded)) {
                    throw new NotFoundException("physical_path");
                }

                var rootVDir = app.VirtualDirectories["/"];
                if (rootVDir != null) {
                    rootVDir.PhysicalPath = physicalPath;
                }

            }

            if (model.application_pool != null) {

                // Change application pool
                if (model.application_pool.id == null) {
                    throw new ApiArgumentException("application_pool.id");
                }

                string poolName = AppPools.AppPoolId.CreateFromUuid(DynamicHelper.Value(model.application_pool.id)).Name;
                ApplicationPool pool = AppPoolHelper.GetAppPool(poolName);

                if(pool != null) {
                    app.ApplicationPoolName = pool.Name;
                }

            }
        }
    }
}
