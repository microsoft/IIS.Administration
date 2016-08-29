// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.VirtualDirectories
{
    using Core.Utils;
    using Web.Administration;
    using System;
    using Core;
    using Applications;
    using Sites;
    using System.IO;
    using System.Dynamic;

    public static class VDirHelper
    {
        private static readonly Fields RefFields = new Fields("location", "id", "path");

        public static VirtualDirectory CreateVDir(Application app, dynamic model)
        {
            if(app == null) {
                throw new ArgumentNullException("app");
            }
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (String.IsNullOrEmpty(DynamicHelper.Value(model.path))) {
                throw new ApiArgumentException("path");
            }
            string physicalPath = DynamicHelper.Value(model.physical_path);
            if (String.IsNullOrEmpty(System.Environment.ExpandEnvironmentVariables(physicalPath))) {
                throw new ApiArgumentException("physical_path");
            }
            if (!Directory.Exists(System.Environment.ExpandEnvironmentVariables(physicalPath))) {
                throw new ApiArgumentException("physical_path", "Directory does not exist.");
            }

            // Create virtual directory using app argument
            VirtualDirectory vdir = app.VirtualDirectories.CreateElement();

            // Initialize virtual directory to default state
            SetDefaults(vdir);

            // Update virtual directory with any specified state
            SetVirtualDirectory(vdir, model);
            
            return vdir;
        }

        public static VirtualDirectory GetVDir(string path, Application app)
        {
            if(app == null || String.IsNullOrEmpty(path)) {
                return null;
            }
            return app.VirtualDirectories[path];
        }

        public static VirtualDirectory UpdateVirtualDirectory(VirtualDirectory vdir, dynamic model)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (vdir == null) {
                throw new ArgumentNullException("vdir");
            }

            SetVirtualDirectory(vdir, model);

            return vdir;
        }

        public static void DeleteVirtualDirectory(VirtualDirectory vdir, Application app)
        {
            if(app == null) {
                return;
            }

            if(app.VirtualDirectories[vdir.Path] != null) {
                app.VirtualDirectories.Remove(vdir);
            }
        }

        public static object ToJsonModel(VirtualDirectory vdir, Application app, Site site, Fields fields = null)
        {
            if (vdir == null) {
                return null;
            }
            if (app == null) {
                throw new ArgumentNullException("app");
            }
            if (site == null) {
                throw new ArgumentNullException("site");
            }

            bool full = fields == null || !fields.HasFields;

            if (fields == null) {
                fields = Fields.All;
            }

            dynamic obj = new ExpandoObject();            
            
            //
            // location
            if (fields.Exists("location")) {
                obj.location = string.Format("{0}{1}{2}", site.Name, app.Path.Equals("/") ? string.Empty : app.Path, vdir.Path);
            }

            //
            // path
            if (fields.Exists("path")) {
                obj.path = vdir.Path;
            }

            //
            // id
            obj.id = new VDirId(vdir.Path, app.Path, site.Id).Uuid;

            //
            // physical_path
            if (fields.Exists("physical_path")) {
                obj.physical_path = vdir.PhysicalPath;
            }

            //
            // username
            if (fields.Exists("username")) {
                obj.username = vdir.UserName;
            }

            //
            // webapp
            if (fields.Exists("webapp")) {
                obj.webapp = ApplicationHelper.ToJsonModelRef(app, site);
            }

            //
            // website
            if (fields.Exists("website")) {
                obj.website = SiteHelper.ToJsonModelRef(site);
            }
            return Core.Environment.Hal.Apply(Defines.Resource.Guid, obj, full);
        }

        public static object ToJsonModelRef(VirtualDirectory vdir, Application app, Site site)
        {
            return ToJsonModel(vdir, app, site, RefFields);
        }


        public static string GetLocation(string id) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentNullException(nameof(id));
            }

            return $"/{Defines.PATH}/{id}";
        }


        private static void SetDefaults(VirtualDirectory vdir)
        {
            VirtualDirectoryDefaults defaults = ManagementUnit.ServerManager.VirtualDirectoryDefaults;

            vdir.LogonMethod = defaults.LogonMethod;
            vdir.Password = defaults.Password;
            vdir.UserName = defaults.UserName;
        }

        private static void SetVirtualDirectory(VirtualDirectory vdir, dynamic model)
        {
            string path = DynamicHelper.Value(model.path);
            if (!string.IsNullOrEmpty(path)) {

                // Make sure path starts with '/'
                if (path[0] != '/') {
                    path = '/' + path;
                }

                vdir.Path = path;
            }

            string physicalPath = DynamicHelper.Value(model.physical_path);

            if(physicalPath != null) {
                if (!Directory.Exists(System.Environment.ExpandEnvironmentVariables(physicalPath))) {
                    throw new ApiArgumentException("physical_path", "Directory does not exist.");
                }
                vdir.PhysicalPath = physicalPath.Replace('/', '\\');
            }

            vdir.LogonMethod = DynamicHelper.To<AuthenticationLogonMethod>(model.auth_logon_method) ?? vdir.LogonMethod;
            vdir.Password = DynamicHelper.Value(model.password) ?? vdir.Password;
            vdir.UserName = DynamicHelper.Value(model.username) ?? vdir.UserName;
        }
    }
}
