// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Files
{
    using Administration.Files;
    using Core.Utils;
    using Sites;
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO;
    using Web.Administration;

    public static class FilesHelper
    {
        private static readonly Fields RefFields = new Fields("name", "id", "type", "path", "physical_path");

        private static IFileProvider _service = FileProvider.Default;
        private static IAccessControl _acessControl = AccessControl.Default;

        public static object ToJsonModel(Site site, string path, Fields fields = null, bool full = true)
        {
            var physicalPath = GetPhysicalPath(site, path);

            if (physicalPath != null) {

                var fileType = GetFileType(site, path, physicalPath);

                switch (fileType) {

                    case FileType.File:
                        return FileToJsonModel(site, path, fields, full);

                    case FileType.Directory:
                        return DirectoryToJsonModel(site, path, fields, full);

                    case FileType.VDir:
                        var app = ResolveApplication(site, path);
                        var vdir = ResolveVdir(site, path);
                        return VdirToJsonModel(new Vdir(site, app, vdir), fields, full);
                }
            }

            return null;
        }

        public static object ToJsonModelRef(Site site, string path, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return ToJsonModel(site, path, RefFields, false);
            }
            else {
                return ToJsonModel(site, path, fields, false);
            }
        }

        internal static object DirectoryToJsonModel(Site site, string path, Fields fields = null, bool full = true)
        {
            if (string.IsNullOrEmpty(path)) {
                throw new ArgumentNullException(nameof(path));
            }

            if (fields == null) {
                fields = Fields.All;
            }

            var physicalPath = GetPhysicalPath(site, path);
            DirectoryInfo directory = null;
            bool? exists = null;

            path = path.Replace('\\', '/');

            dynamic obj = new ExpandoObject();
            var FileId = new FileId(site.Id, path);

            //
            // name
            if (fields.Exists("name")) {
                obj.name = new DirectoryInfo(path).Name;
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
            // created
            if (fields.Exists("created")) {
                directory = directory ?? new DirectoryInfo(physicalPath);
                exists = exists ?? directory.Exists;
                obj.created = exists.Value ? (object)directory.CreationTimeUtc : null;
            }

            //
            // last_modified
            if (fields.Exists("last_modified")) {
                directory = directory ?? new DirectoryInfo(physicalPath);
                exists = exists ?? directory.Exists;
                obj.last_modified = exists.Value ? (object)directory.LastWriteTimeUtc : null;
            }

            //
            // parent
            if (fields.Exists("parent")) {
                obj.parent = GetParentJsonModelRef(site, path, fields.Filter("parent"));
            }

            //
            // website
            if (fields.Exists("website")) {
                obj.website = SiteHelper.ToJsonModelRef(site, fields.Filter("website"));
            }

            //
            // file_info
            if (fields.Exists("file_info")) {
                obj.file_info = Administration.Files.FilesHelper.ToJsonModelRef(physicalPath, fields.Filter("file_info"));
            }

            return Core.Environment.Hal.Apply(Defines.DirectoriesResource.Guid, obj, full);
        }

        internal static object DirectoryToJsonModelRef(Site site, string path, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return DirectoryToJsonModel(site, path, RefFields, false);
            }
            else {
                return DirectoryToJsonModel(site, path, fields, false);
            }
        }

        internal static object FileToJsonModel(Site site, string path, Fields fields = null, bool full = true)
        {
            if (string.IsNullOrEmpty(path)) {
                throw new ArgumentNullException(nameof(path));
            }

            if (fields == null) {
                fields = Fields.All;
            }

            path = path.Replace('\\', '/');
            FileInfo file = new FileInfo(GetPhysicalPath(site, path));
            bool? exists = null;

            dynamic obj = new ExpandoObject();
            var FileId = new FileId(site.Id, path);

            //
            // name
            if (fields.Exists("name")) {
                obj.name = file.Name;
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
            // path
            if (fields.Exists("path")) {
                obj.path = path;
            }

            //
            // size
            if (fields.Exists("size")) {
                exists = exists ?? file.Exists;
                obj.size = exists.Value ? file.Length : 0;
            }

            //
            // created
            if (fields.Exists("created")) {
                exists = exists ?? file.Exists;
                obj.created = exists.Value ? (object)file.CreationTimeUtc : null;
            }

            //
            // last_modified
            if (fields.Exists("last_modified")) {
                exists = exists ?? file.Exists;
                obj.last_modified = exists.Value ? (object)file.LastWriteTimeUtc : null;
            }

            //
            // parent
            if (fields.Exists("parent")) {
                obj.parent = GetParentJsonModelRef(site, path, fields.Filter("parent"));
            }

            //
            // website
            if (fields.Exists("website")) {
                obj.website = SiteHelper.ToJsonModelRef(site, fields.Filter("website"));
            }

            //
            // file_info
            if (fields.Exists("file_info")) {
                obj.file_info = Administration.Files.FilesHelper.ToJsonModelRef(file.FullName, fields.Filter("file_info"));
            }


            return Core.Environment.Hal.Apply(Defines.FilesResource.Guid, obj, full);
        }

        internal static object FileToJsonModelRef(Site site, string path, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return FileToJsonModel(site, path, RefFields, false);
            }
            else {
                return FileToJsonModel(site, path, fields, false);
            }
        }

        internal static object VdirToJsonModel(Vdir vdir, Fields fields = null, bool full = true)
        {
            if (vdir == null) {
                return null;
            }

            if (fields == null) {
                fields = Fields.All;
            }

            var physicalPath = GetPhysicalPath(vdir.Site, vdir.Path);
            DirectoryInfo directory = null;
            bool? exists = null;

            dynamic obj = new ExpandoObject();
            var FileId = new FileId(vdir.Site.Id, vdir.Path);

            //
            // name
            if (fields.Exists("name")) {
                obj.name = vdir.Path.TrimStart('/');
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
                obj.path = vdir.Path.Replace('\\', '/');
            }

            //
            // created
            if (fields.Exists("created")) {
                directory = directory ?? new DirectoryInfo(physicalPath);
                exists = exists ?? directory.Exists;
                obj.created = exists.Value ? (object)directory.CreationTimeUtc : null;
            }

            //
            // last_modified
            if (fields.Exists("last_modified")) {
                directory = directory ?? new DirectoryInfo(physicalPath);
                exists = exists ?? directory.Exists;
                obj.last_modified = exists.Value ? (object)directory.LastWriteTimeUtc : null;
            }

            //
            // parent
            if (fields.Exists("parent")) {
                obj.parent = GetParentVdirJsonModelRef(vdir, fields.Filter("parent"));
            }

            //
            // website
            if (fields.Exists("website")) {
                obj.website = SiteHelper.ToJsonModelRef(vdir.Site, fields.Filter("website"));
            }

            //
            // file_info
            if (fields.Exists("file_info")) {
                obj.file_info = Administration.Files.FilesHelper.ToJsonModelRef(physicalPath, fields.Filter("file_info"));
            }

            return Core.Environment.Hal.Apply(Defines.DirectoriesResource.Guid, obj, full);
        }

        internal static object VdirToJsonModelRef(Vdir vdir, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return VdirToJsonModel(vdir, RefFields, false);
            }
            else {
                return VdirToJsonModel(vdir, fields, false);
            }
        }

        public static string GetLocation(string id)
        {
            return $"/{Defines.FILES_PATH}/{id}";
        }

        internal static FileType GetFileType(Site site, string path, string physicalPath)
        {
            // A virtual directory is a directory who's physical path is not the combination of the sites physical path and the relative path from the sites root
            // and has same virtual path as app + vdir path
            var app = ResolveApplication(site, path);
            var vdir = ResolveVdir(site, path);
            
            var differentPhysicalPath = !Path.Combine(GetPhysicalPath(site), path.Replace('/', '\\').TrimStart('\\')).Equals(physicalPath, StringComparison.OrdinalIgnoreCase);

            if (path == "/" || differentPhysicalPath && IsExactVdirPath(site, app, vdir, path)) {
                return FileType.VDir;
            }
            else if (Directory.Exists(physicalPath)) {
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
                var matchingPrefix = PrefixSegments(app.Path, path);
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
                    var matchingPrefix = PrefixSegments(vdir.Path, testPath);
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

        public static string GetPhysicalPath(Site site, string path)
        {
            var app = ResolveApplication(site, path);
            var vdir = ResolveVdir(site, path);
            string physicalPath = null;

            if (vdir != null) {
                var suffix = path.TrimStart(app.Path.TrimEnd('/'), StringComparison.OrdinalIgnoreCase).TrimStart(vdir.Path.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
                physicalPath = Path.Combine(vdir.PhysicalPath, suffix.Trim(PathUtil.SEPARATORS).Replace('/', Path.DirectorySeparatorChar));
                physicalPath = PathUtil.GetFullPath(physicalPath);
            }

            return physicalPath;
        }

        internal static bool IsValidPath(string path)
        {
            if (path == null || !path.StartsWith("/")) {
                return false;
            }

            var segs = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            foreach (var seg in segs) {
                if (seg.IndexOfAny(PathUtil.InvalidFileNameChars) != -1) {
                    return false;
                }
            }

            string absolute = null;

            try {
                absolute = PathUtil.GetFullPath(path);
            }
            catch (ArgumentException) {
                //
                // Argument exception for invalid paths such as '////' (Invalid network share format)
                return false;
            }

            var slashIndex = absolute.IndexOf(Path.DirectorySeparatorChar);
            if (!absolute.Substring(slashIndex, absolute.Length - slashIndex).Equals(path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar))) {
                return false;
            }

            return true;
        }

        internal static IEnumerable<Vdir> GetVdirs(Site site, string path)
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
                    break;
                }
            }
            return vdirs;
        }


        
        private static string GetPhysicalPath(Site site)
        {
            if (site == null) {
                throw new ArgumentNullException(nameof(site));
            }

            string root = null;
            var rootApp = site.Applications["/"];
            if (rootApp != null && rootApp.VirtualDirectories["/"] != null) {
                root = PathUtil.GetFullPath(rootApp.VirtualDirectories["/"].PhysicalPath);
            }
            return root;
        }

        private static object GetParentJsonModelRef(Site site, string path, Fields fields = null)
        {
            object parent = null;
            if (path != "/") {
                var parentPath = PathUtil.RemoveLastSegment(path);
                var parentApp = ResolveApplication(site, parentPath);
                var parentVdir = ResolveVdir(site, parentPath);

                if (IsExactVdirPath(site, parentApp, parentVdir, parentPath)) {
                    parent = VdirToJsonModelRef(new Vdir(site, parentApp, parentVdir));
                }
                else {
                    var parentPhysicalPath = GetPhysicalPath(site, parentPath);
                    parent = Directory.Exists(parentPhysicalPath) ? DirectoryToJsonModelRef(site, parentPath, fields) : null;
                }
            }
            return parent;
        }

        private static object GetParentVdirJsonModelRef(Vdir vdir, Fields fields = null)
        {
            object ret = null;

            if (vdir.VirtualDirectory.Path != "/") {
                var rootVdir = vdir.Application.VirtualDirectories["/"];
                ret = rootVdir == null ? null : VdirToJsonModelRef(new Vdir(vdir.Site, vdir.Application, rootVdir));
            }
            else if (vdir.Application.Path != "/") {
                var rootApp = vdir.Site.Applications["/"];
                var rootVdir = rootApp == null ? null : rootApp.VirtualDirectories["/"];
                ret = rootApp == null || rootVdir == null ? null : VdirToJsonModelRef(new Vdir(vdir.Site, rootApp, rootVdir), fields);
            }
            else {
                ret = null;
            }

            return ret;
        }

        private static int PrefixSegments(string prefix, string path, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase)
        {
            if (prefix == null) {
                throw new ArgumentNullException(nameof(prefix));
            }
            if (path == null) {
                throw new ArgumentNullException(nameof(path));
            }
            if (!PathUtil.IsPathRooted(prefix) || !PathUtil.IsPathRooted(path)) {
                throw new ArgumentException("Paths must be rooted.");
            }

            var prefixParts = prefix.TrimEnd(PathUtil.SEPARATORS).Split(PathUtil.SEPARATORS);
            var pathParts = path.TrimEnd(PathUtil.SEPARATORS).Split(PathUtil.SEPARATORS);

            if (prefixParts.Length > pathParts.Length) {
                return -1;
            }

            int index = 0;
            while (pathParts.Length > index && prefixParts.Length > index && prefixParts[index].Equals(pathParts[index], stringComparison)) {
                index++;
            }

            if (prefixParts.Length > index) {
                return -1;
            }

            return index == 0 ? -1 : index;
        }

        private static string TrimStart(this string val, string prefix, StringComparison stringComparision = StringComparison.Ordinal)
        {
            if (val.StartsWith(prefix, stringComparision)) {
                val = val.Remove(0, prefix.Length);
            }
            return val;
        }
    }
}
