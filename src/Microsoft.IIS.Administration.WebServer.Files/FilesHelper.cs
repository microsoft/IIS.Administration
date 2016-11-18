// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Files
{
    using Administration.Files;
    using Core;
    using Core.Utils;
    using Sites;
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO;
    using Web.Administration;

    public static class FilesHelper
    {
        private static readonly Fields DirectoryRefFields = new Fields("name", "id", "type", "path", "physical_path");
        private static readonly Fields FileRefFields = new Fields("name", "id", "type", "path", "physical_path");

        private static FileService _service = new FileService();

        public static object DirectoryToJsonModel(Site site, string path, Fields fields = null, bool full = true)
        {
            if (string.IsNullOrEmpty(path)) {
                throw new ArgumentNullException(nameof(path));
            }

            if (fields == null) {
                fields = Fields.All;
            }

            path = path.Replace('\\', '/');
            var physicalPath = GetPhysicalPath(site, path);

            dynamic obj = new ExpandoObject();
            var FileId = new FileId(site.Id, path);
            var dirInfo = _service.GetDirectoryInfo(physicalPath);

            if (!_service.DirectoryExists(physicalPath)) {
                return null;
            }

            //
            // name
            if (fields.Exists("name")) {
                obj.name = dirInfo.Name;
            }

            //
            // id
            if (fields.Exists("id")) {
                obj.id = FileId.Uuid;
            }

            //
            // type
            if (fields.Exists("type")) {
                obj.type = Enum.GetName(typeof(FileType), FileType.Directory).ToLower();
            }

            //
            // path
            if (fields.Exists("path")) {
                obj.path = path;
            }

            //
            // physical_path
            if (fields.Exists("physical_path")) {
                obj.physical_path = physicalPath;
            }

            //
            // created
            if (fields.Exists("created")) {
                obj.created = dirInfo.CreationTimeUtc;
            }

            //
            // last_modified
            if (fields.Exists("last_modified")) {
                obj.last_modified = dirInfo.LastWriteTimeUtc;
            }

            //
            // total_files
            if (fields.Exists("total_files")) {
                obj.total_files = dirInfo.GetFiles().Length + dirInfo.GetDirectories().Length + GetChildVirtualDirectories(site, path).Count;
            }

            //
            // parent
            if (fields.Exists("parent")) {
                obj.parent = GetParentJsonModelRef(site, path);
            }

            //
            // website
            if (fields.Exists("website")) {
                obj.website = SiteHelper.ToJsonModelRef(site);
            }

            return Core.Environment.Hal.Apply(Defines.DirectoriesResource.Guid, obj, full);
        }

        public static object DirectoryToJsonModelRef(Site site, string path, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return DirectoryToJsonModel(site, path, DirectoryRefFields, false);
            }
            else {
                return DirectoryToJsonModel(site, path, fields, false);
            }
        }

        public static object FileToJsonModel(Site site, string path, Fields fields = null, bool full = true)
        {
            if (string.IsNullOrEmpty(path)) {
                throw new ArgumentNullException(nameof(path));
            }

            if (fields == null) {
                fields = Fields.All;
            }

            path = path.Replace('\\', '/');
            var physicalPath = GetPhysicalPath(site, path);

            dynamic obj = new ExpandoObject();
            var FileId = new FileId(site.Id, path);
            var fileInfo = _service.GetFileInfo(physicalPath);
            var fileVersionInfo = _service.GetFileVersionInfo(fileInfo.FullName);

            //
            // name
            if (fields.Exists("name")) {
                obj.name = fileInfo.Name;
            }

            //
            // id
            if (fields.Exists("id")) {
                obj.id = FileId.Uuid;
            }

            //
            // type
            if (fields.Exists("type")) {
                obj.type = Enum.GetName(typeof(FileType), FileType.File).ToLower();
            }

            //
            // length
            if (fields.Exists("length")) {
                obj.length = fileInfo.Length;
            }

            //
            // created
            if (fields.Exists("created")) {
                obj.created = fileInfo.CreationTimeUtc;
            }

            //
            // last_modified
            if (fields.Exists("last_modified")) {
                obj.last_modified = fileInfo.LastWriteTimeUtc;
            }

            //
            // e_tag
            if (fields.Exists("e_tag")) {
                obj.e_tag = ETag.Create(fileInfo).Value;
            }

            //
            // path
            if (fields.Exists("path")) {
                obj.path = path.Replace('\\', '/');
            }

            //
            // physical_path
            if (fields.Exists("physical_path")) {
                obj.physical_path = physicalPath;
            }

            //
            // version
            if (fields.Exists("version") && fileVersionInfo.FileVersion != null) {
                obj.version = fileVersionInfo.FileVersion;
            }

            //
            // parent
            if (fields.Exists("parent")) {
                obj.parent = GetParentJsonModelRef(site, path);
            }

            //
            // website
            if (fields.Exists("website")) {
                obj.website = SiteHelper.ToJsonModelRef(site);
            }


            return Core.Environment.Hal.Apply(Defines.FilesResource.Guid, obj, full);
        }

        internal static object VirtualDirectoryToJsonModel(Vdir fullVdir, Fields fields = null, bool full = true)
        {
            if (fullVdir == null) {
                return null;
            }

            if (fields == null) {
                fields = Fields.All;
            }

            var physicalPath = GetPhysicalPath(fullVdir.Site, fullVdir.Path);

            dynamic obj = new ExpandoObject();
            var FileId = new FileId(fullVdir.Site.Id, fullVdir.Path);
            var dirInfo = _service.GetDirectoryInfo(physicalPath);

            //
            // name
            if (fields.Exists("name")) {
                obj.name = fullVdir.Path == "/" ? fullVdir.Site.Name : fullVdir.Path.TrimStart('/');
            }

            //
            // id
            if (fields.Exists("id")) {
                obj.id = FileId.Uuid;
            }

            //
            // type
            if (fields.Exists("type")) {
                obj.type = Enum.GetName(typeof(FileType), FileType.VDir).ToLower();
            }

            //
            // path
            if (fields.Exists("path")) {
                obj.path = fullVdir.Path.Replace('\\', '/');
            }

            //
            // physical_path
            if (fields.Exists("physical_path")) {
                obj.physical_path = physicalPath;
            }

            //
            // created
            if (fields.Exists("created")) {
                obj.created = dirInfo.CreationTimeUtc;
            }

            //
            // last_modified
            if (fields.Exists("last_modified")) {
                obj.last_modified = dirInfo.LastWriteTimeUtc;
            }

            //
            // total_files
            if (fields.Exists("total_files")) {
                obj.total_files = dirInfo.GetFiles().Length + dirInfo.GetDirectories().Length + GetChildVirtualDirectories(fullVdir.Site, fullVdir.Path).Count;
            }

            //
            // parent
            if (fields.Exists("parent")) {
                if (fullVdir.VirtualDirectory.Path != "/") {
                    var rootVdir = fullVdir.Application.VirtualDirectories["/"];
                    obj.parent = rootVdir == null ? null : VirtualDirectoryToJsonModelRef(new Vdir(fullVdir.Site, fullVdir.Application, rootVdir));
                }
                else if (fullVdir.Application.Path != "/") {
                    var rootApp = fullVdir.Site.Applications["/"];
                    var rootVdir = rootApp == null ? null : rootApp.VirtualDirectories["/"];
                    obj.parent = rootApp == null || rootVdir == null ? null : VirtualDirectoryToJsonModel(new Vdir(fullVdir.Site, rootApp, rootVdir));
                }
                else {
                    obj.parent = null;
                }
            }

            //
            // website
            if (fields.Exists("website")) {
                obj.website = SiteHelper.ToJsonModelRef(fullVdir.Site);
            }

            return Core.Environment.Hal.Apply(Defines.DirectoriesResource.Guid, obj, full);
        }

        internal static object VirtualDirectoryToJsonModelRef(Vdir vdir, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return VirtualDirectoryToJsonModel(vdir, DirectoryRefFields, false);
            }
            else {
                return VirtualDirectoryToJsonModel(vdir, fields, false);
            }
        }

        public static object FileToJsonModelRef(Site site, string path, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return FileToJsonModel(site, path, FileRefFields, false);
            }
            else {
                return FileToJsonModel(site, path, fields, false);
            }
        }

        public static string UpdateFile(dynamic model, string physicalPath)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            string name = DynamicHelper.Value(model.name);

            if (name != null) {
                if (!IsValidFileName(name)) {
                    throw new ApiArgumentException("name");
                }
                var newPath = Path.Combine(_service.GetParentPath(physicalPath), name);
                if (_service.FileExists(newPath)) {
                    throw new AlreadyExistsException("name");
                }
                _service.MoveFile(physicalPath, newPath);
                physicalPath = newPath;
            }

            return physicalPath;
        }

        public static string UpdateDirectory(dynamic model, string directoryPath)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            string name = DynamicHelper.Value(model.name);

            if (name != null) {
                if (!IsValidFileName(name)) {
                    throw new ApiArgumentException("name");
                }
                if (_service.GetParentPath(directoryPath) != null) {
                    var newPath = Path.Combine(_service.GetParentPath(directoryPath), name);
                    if (_service.DirectoryExists(newPath)) {
                        throw new AlreadyExistsException("name");
                    }
                    _service.MoveDirectory(directoryPath, newPath);
                    directoryPath = newPath;
                }
            }

            return directoryPath;
        }

        public static string GetLocation(string id)
        {
            return $"/{Defines.FILES_PATH}/{id}";
        }

        public static FileType GetFileType(Site site, string path, string physicalPath)
        {
            // A virtual directory is a directory who's physical path is not the combination of the sites physical path and the relative path from the sites root
            // and has same virtual path as app + vdir path
            var app = ResolveApplication(site, path);
            var vdir = ResolveVdir(site, path);
            
            var differentPhysicalPath = !Path.Combine(GetPhysicalPath(site), path.Replace('/', '\\').TrimStart('\\')).Equals(physicalPath, StringComparison.OrdinalIgnoreCase);

            if (path == "/" || differentPhysicalPath && IsExactVdirPath(site, app, vdir, path)) {
                return FileType.VDir;
            }
            else if (_service.DirectoryExists(physicalPath)) {
                return FileType.Directory;
            }

            return FileType.File;
        }

        public static bool IsExactVdirPath(Site site, Application app, VirtualDirectory vdir, string path)
        {
            path = path.TrimEnd('/');
            var virtualPath = app.Path.TrimEnd('/') + vdir.Path.TrimEnd('/');
            return path.Equals(virtualPath, StringComparison.OrdinalIgnoreCase);
        }

        public static Application ResolveApplication(Site site, string path)
        {
            if (string.IsNullOrEmpty(path)) {
                throw new ArgumentNullException(nameof(path));
            }
            if (site == null) {
                throw new ArgumentNullException(nameof(site));
            }
            
            Application parentApp = null;
            var maxMatch = 0;
            foreach (var app in site.Applications) {
                var matchingPrefix = PathUtil.PrefixSegments(app.Path, path);
                if (matchingPrefix > maxMatch) {
                    parentApp = app;
                    maxMatch = matchingPrefix;
                }
            }
            return parentApp;
        }

        public static VirtualDirectory ResolveVdir(Site site, string path)
        {
            if (string.IsNullOrEmpty(path)) {
                throw new ArgumentNullException(nameof(path));
            }
            if (site == null) {
                throw new ArgumentNullException(nameof(site));
            }

            var parentApp = ResolveApplication(site, path);
            VirtualDirectory parentVdir = null;
            if (parentApp != null) {
                var maxMatch = 0;
                var testPath = path.TrimStart(parentApp.Path.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
                testPath = testPath == string.Empty ? "/" : testPath;
                foreach (var vdir in parentApp.VirtualDirectories) {
                    var matchingPrefix = PathUtil.PrefixSegments(vdir.Path, testPath);
                    if (matchingPrefix > maxMatch) {
                        parentVdir = vdir;
                        maxMatch = matchingPrefix;
                    }
                }
            }
            return parentVdir;
        }

        internal static Vdir ResolveFullVdir(Site site, string path)
        {
            VirtualDirectory vdir = null;

            var app = ResolveApplication(site, path);
            if (app != null) {
                vdir = ResolveVdir(site, path);
            }

            return vdir == null ? null : new Vdir(site, app, vdir);
        }

        public static string GetPhysicalPath(Site site)
        {
            if (site == null) {
                throw new ArgumentNullException(nameof(site));
            }

            string root = null;
            var rootApp = site.Applications["/"];
            if (rootApp != null && rootApp.VirtualDirectories["/"] != null) {
                root = rootApp.VirtualDirectories["/"].PhysicalPath;
            }
            return root;
        }

        public static string GetPhysicalPath(Site site, string path)
        {
            var app = ResolveApplication(site, path);
            var vdir = ResolveVdir(site, path);
            string physicalPath = null;

            if (vdir != null) {
                var suffix = path.TrimStart(app.Path.TrimEnd('/'), StringComparison.OrdinalIgnoreCase).TrimStart(vdir.Path.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
                physicalPath = Path.Combine(vdir.PhysicalPath, suffix.Trim(PathUtil.SEPARATORS).Replace('/', Path.DirectorySeparatorChar));
            }

            return System.Environment.ExpandEnvironmentVariables(physicalPath);
        }

        internal static List<Vdir> GetChildVirtualDirectories(Site site, string path)
        {
            var vdirs = new List<Vdir>();

            if (path == "/") {
                foreach (var app in site.Applications) {
                    if (app.Path != "/") {
                        foreach (var vdir in app.VirtualDirectories) {
                            if (vdir.Path == "/") {
                                vdirs.Add(new Vdir(site, app, vdir));
                                break;
                            }
                        }
                    }
                }
            }

            foreach (var app in site.Applications) {
                if (app.Path.Equals(path, StringComparison.OrdinalIgnoreCase)) {
                    foreach (var vdir in app.VirtualDirectories) {
                        if (vdir.Path != "/") {
                            vdirs.Add(new Vdir(site, app, vdir));
                        }
                    }
                }
            }
            return vdirs;
        }

        public static bool IsValidFileName(string name)
        {
            return !string.IsNullOrEmpty(name) &&
                        name.IndexOfAny(Path.GetInvalidFileNameChars()) == -1 &&
                        !name.EndsWith(".");
        }




        private static object GetParentJsonModelRef(Site site, string path)
        {
            object parent = null;
            if (path != "/") {
                var parentPath = PathUtil.RemoveLastSegment(path);
                var parentApp = ResolveApplication(site, parentPath);
                var parentVdir = ResolveVdir(site, parentPath);

                if (IsExactVdirPath(site, parentApp, parentVdir, parentPath)) {
                    parent = VirtualDirectoryToJsonModelRef(new Vdir(site, parentApp, parentVdir));
                }
                else {
                    var parentPhysicalPath = GetPhysicalPath(site, parentPath);
                    parent = _service.DirectoryExists(parentPhysicalPath) ? DirectoryToJsonModelRef(site, parentPath) : null;
                }
            }
            return parent;
        }
    }
}
