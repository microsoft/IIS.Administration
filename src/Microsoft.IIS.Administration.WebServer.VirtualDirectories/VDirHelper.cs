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
    using Newtonsoft.Json.Linq;
    using Files;

    public static class VDirHelper
    {
        private static readonly Fields RefFields = new Fields("location", "id", "path");
        private const string PasswordAttribute = "password";
        private const string UserNameAttribute = "userName";

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

        internal static object ToJsonModel(VirtualDirectory vdir, Application app, Site site, Fields fields = null, bool full = true)
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
            // identity
            if (fields.Exists("identity")) {
                obj.identity = new {
                    username = vdir.UserName,
                    logon_method = FriendlyLogonMethod(vdir.LogonMethod)
                };
            }

            //
            // webapp
            if (fields.Exists("webapp")) {
                obj.webapp = ApplicationHelper.ToJsonModelRef(app, site, fields.Filter("webapp"));
            }

            //
            // website
            if (fields.Exists("website")) {
                obj.website = SiteHelper.ToJsonModelRef(site, fields.Filter("website"));
            }
            return Core.Environment.Hal.Apply(Defines.Resource.Guid, obj, full);
        }

        public static object ToJsonModelRef(VirtualDirectory vdir, Application app, Site site, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return ToJsonModel(vdir, app, site, RefFields, false);
            }
            else {
                return ToJsonModel(vdir, app, site, fields, false);
            }
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

                vdir.PhysicalPath = physicalPath;
            }

            if (model.identity != null) {
                var identity = model.identity;

                if (!(model.identity is JObject)) {
                    throw new ApiArgumentException("model.identity", ApiArgumentException.EXPECTED_OBJECT);
                }

                DynamicHelper.If((object)identity.logon_method, v => vdir.LogonMethod = ToLogonMethod(v));

                string username = DynamicHelper.Value(identity.username);
                string password = DynamicHelper.Value(identity.password);

                if (password != null) {
                    vdir.Password = password;
                }

                if (username != null) {

                    vdir.UserName = username;

                    if (username == string.Empty) {
                        vdir.GetAttribute(UserNameAttribute).Delete();
                        vdir.GetAttribute(PasswordAttribute).Delete();
                    }
                }
            }
        }

        private static string FriendlyLogonMethod(AuthenticationLogonMethod logonMethod)
        {
            string name = Enum.GetName(typeof(AuthenticationLogonMethod), logonMethod);
            if (name.Equals("cleartext", StringComparison.OrdinalIgnoreCase)) {
                name = "network_cleartext";
            }

            return name.ToLower();
        }

        private static AuthenticationLogonMethod ToLogonMethod(string val)
        {
            AuthenticationLogonMethod ret;
            if (val.Equals("network_cleartext", StringComparison.OrdinalIgnoreCase)) {
                val = "cleartext";
            }
            if (!Enum.TryParse(val, true, out ret)) {
                throw new ApiArgumentException("logon_method");
            }
            return ret;
        }
    }
}
