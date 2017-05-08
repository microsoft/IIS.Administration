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

    public class FilesHelper
    {
        internal static readonly Fields RefFields = new Fields("name", "id", "type", "path", "physical_path");

        private IFileProvider _fileProvider;
        private Administration.Files.FilesHelper _filesHelper;

        public FilesHelper(IFileProvider fileProvider)
        {
            _fileProvider = fileProvider;
            _filesHelper = new Administration.Files.FilesHelper(_fileProvider);
        }

        public object ToJsonModel(Site site, string path, Fields fields = null, bool full = true)
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
                    case FileType.Application:
                        var app = ResolveApplication(site, path);
                        var vdir = ResolveVdir(site, path);
                        return VdirToJsonModel(new Vdir(site, app, vdir), fields, full);
                }
            }

            return null;
        }

        public object ToJsonModelRef(Site site, string path, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return ToJsonModel(site, path, RefFields, false);
            }
            else {
                return ToJsonModel(site, path, fields, false);
            }
        }

        //
        // Allow caller to pass parent JSON model to optimize performance for serializing entire directories
        internal object DirectoryToJsonModel(Site site, string path, Fields fields = null, bool full = true, object parent = null)
        {
            if (string.IsNullOrEmpty(path)) {
                throw new ArgumentNullException(nameof(path));
            }

            if (fields == null) {
                fields = Fields.All;
            }
            
            var directory = _fileProvider.GetDirectory(GetPhysicalPath(site, path));

            path = path.Replace('\\', '/');

            dynamic obj = new ExpandoObject();
            var FileId = new FileId(site.Id, path);

            //
            // name
            if (fields.Exists("name")) {
                obj.name = directory.Name;
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
            // parent
            if (fields.Exists("parent")) {
                obj.parent = parent ?? GetParentJsonModelRef(site, path, fields.Filter("parent"));
            }

            //
            // website
            if (fields.Exists("website")) {
                obj.website = SiteHelper.ToJsonModelRef(site, fields.Filter("website"));
            }

            //
            // file_info
            if (fields.Exists("file_info")) {
                obj.file_info = _filesHelper.ToJsonModelRef(directory, fields.Filter("file_info"));
            }

            return Core.Environment.Hal.Apply(Defines.DirectoriesResource.Guid, obj, full);
        }

        internal object DirectoryToJsonModelRef(Site site, string path, Fields fields = null, object parent = null)
        {
            if (fields == null || !fields.HasFields) {
                return DirectoryToJsonModel(site, path, RefFields, false, parent);
            }
            else {
                return DirectoryToJsonModel(site, path, fields, false, parent);
            }
        }

        //
        // Allow caller to pass parent JSON model to optimize performance for serializing entire directories
        internal object FileToJsonModel(Site site, string path, Fields fields = null, bool full = true, object parent = null)
        {
            if (string.IsNullOrEmpty(path)) {
                throw new ArgumentNullException(nameof(path));
            }

            if (fields == null) {
                fields = Fields.All;
            }

            path = path.Replace('\\', '/');
            var file = _fileProvider.GetFile(GetPhysicalPath(site, path));

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
            // parent
            if (fields.Exists("parent")) {
                obj.parent = parent ?? GetParentJsonModelRef(site, path, fields.Filter("parent"));
            }

            //
            // website
            if (fields.Exists("website")) {
                obj.website = SiteHelper.ToJsonModelRef(site, fields.Filter("website"));
            }

            //
            // file_info
            if (fields.Exists("file_info")) {
                obj.file_info = _filesHelper.ToJsonModelRef(file, fields.Filter("file_info"));
            }


            return Core.Environment.Hal.Apply(Defines.FilesResource.Guid, obj, full);
        }

        internal object FileToJsonModelRef(Site site, string path, Fields fields = null, object parent = null)
        {
            if (fields == null || !fields.HasFields) {
                return FileToJsonModel(site, path, RefFields, false, parent);
            }
            else {
                return FileToJsonModel(site, path, fields, false, parent);
            }
        }

        internal object VdirToJsonModel(Vdir vdir, Fields fields = null, bool full = true, object parent = null)
        {
            if (vdir == null) {
                return null;
            }

            if (fields == null) {
                fields = Fields.All;
            }
            


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
                obj.type = Enum.GetName(typeof(FileType), Vdir.GetVdirType(vdir.VirtualDirectory)).ToLower();
            }

            //
            // path
            if (fields.Exists("path")) {
                obj.path = vdir.Path.Replace('\\', '/');
            }

            //
            // parent
            if (fields.Exists("parent")) {
                obj.parent = parent ?? GetParentVdirJsonModelRef(vdir, fields.Filter("parent"));
            }

            //
            // website
            if (fields.Exists("website")) {
                obj.website = SiteHelper.ToJsonModelRef(vdir.Site, fields.Filter("website"));
            }

            //
            // file_info
            if (fields.Exists("file_info")) {

                string physicalPath = GetPhysicalPath(vdir.Site, vdir.Path);

                //
                // Virtual directory can be at an arbitrary path from which we can not retrieve the file info
                // Serializing children (parent != null) should not throw
                if (parent != null && !_fileProvider.IsAccessAllowed(physicalPath, FileAccess.Read)) {
                    obj.file_info = null;
                }
                else {
                    obj.file_info = _filesHelper.ToJsonModelRef(_fileProvider.GetDirectory(physicalPath), fields.Filter("file_info"));
                }
            }

            return Core.Environment.Hal.Apply(Defines.DirectoriesResource.Guid, obj, full);
        }

        internal object VdirToJsonModelRef(Vdir vdir, Fields fields = null, object parent = null)
        {
            if (fields == null || !fields.HasFields) {
                return VdirToJsonModel(vdir, RefFields, false, parent);
            }
            else {
                return VdirToJsonModel(vdir, fields, false, parent);
            }
        }

        public static string GetLocation(string id)
        {
            return $"/{Defines.FILES_PATH}/{id}";
        }

        internal FileType GetFileType(Site site, string path, string physicalPath)
        {
            // A virtual directory is a directory who's physical path is not the combination of the sites physical path and the relative path from the sites root
            // and has same virtual path as app + vdir path
            var app = ResolveApplication(site, path);
            var vdir = ResolveVdir(site, path);
            
            var differentPhysicalPath = !Path.Combine(GetPhysicalPath(site), path.Replace('/', '\\').TrimStart('\\')).Equals(physicalPath, StringComparison.OrdinalIgnoreCase);

            if (path == "/" || differentPhysicalPath && IsExactVdirPath(site, app, vdir, path)) {
                return Vdir.GetVdirType(vdir);
            }
            else if (_fileProvider.GetDirectory(physicalPath).Exists) {
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
                var testPath = TrimStart(path, parentApp.Path.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
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
            // If path != valid return null
            // need to normalize path

            var app = ResolveApplication(site, path);
            var vdir = ResolveVdir(site, path);
            string physicalPath = null;

            if (vdir != null) {
                var suffix = TrimStart(TrimStart(path, app.Path.TrimEnd('/'), StringComparison.OrdinalIgnoreCase), vdir.Path.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
                physicalPath = Path.Combine(vdir.PhysicalPath, suffix.Trim(PathUtil.SEPARATORS).Replace('/', Path.DirectorySeparatorChar));
                physicalPath = PathUtil.GetFullPath(physicalPath);
            }

            return physicalPath;
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

        internal static string NormalizeVirtualPath(string path)
        {
            if (string.IsNullOrEmpty(path) || path[0] != '/') {
                throw new ArgumentException(nameof(path));
            }

            return new Uri("file://" + path).LocalPath;
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

        private object GetParentJsonModelRef(Site site, string path, Fields fields = null)
        {
            object parent = null;
            if (path != "/") {
                var parentPath = RemoveLastSegment(path);
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

        private object GetParentVdirJsonModelRef(Vdir vdir, Fields fields = null)
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

        private string RemoveLastSegment(string path)
        {
            if (path == null) {
                throw new ArgumentNullException(nameof(path));
            }
            if (!path.StartsWith("/") || path == "/") {
                throw new ArgumentException(nameof(path));
            }

            var parts = path.TrimEnd(PathUtil.SEPARATORS).Split(PathUtil.SEPARATORS);
            parts[parts.Length - 1] = string.Empty;
            var ret = string.Join("/", parts);
            return ret == "/" ? ret : ret.TrimEnd('/');
        }

        private static string TrimStart(string val, string prefix, StringComparison stringComparision = StringComparison.Ordinal)
        {
            if (val.StartsWith(prefix, stringComparision)) {
                val = val.Remove(0, prefix.Length);
            }
            return val;
        }
    }
}
