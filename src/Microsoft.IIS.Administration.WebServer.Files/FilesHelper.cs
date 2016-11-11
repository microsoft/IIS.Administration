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
        private static readonly Fields DirectoryRefFields = new Fields("name", "id", "type", "path", "physical_path");
        private static readonly Fields FileRefFields = new Fields("name", "id", "type", "path", "physical_path");

        public static object DirectoryToJsonModel(Site site, string path, Fields fields = null, bool full = true)
        {
            if (string.IsNullOrEmpty(path)) {
                throw new ArgumentNullException(nameof(path));
            }

            if (fields == null) {
                fields = Fields.All;
            }

            var physicalPath = GetPhysicalPath(site, path);

            dynamic obj = new ExpandoObject();
            var FileId = new FileId(site.Id, path);
            DirectoryInfo dirInfo = new DirectoryInfo(physicalPath);

            //
            // name
            if (fields.Exists("name")) {
                obj.name = path.Replace('\\', '/').TrimStart('/');
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
                obj.path = path.Replace('\\', '/');
            }

            //
            // physical_path
            if (fields.Exists("physical_path")) {
                obj.physical_path = physicalPath;
            }

            //
            // last_modified
            if (fields.Exists("last_modified")) {
                obj.last_modified = dirInfo.LastWriteTimeUtc;
            }

            //
            // creation_date
            if (fields.Exists("creation_date")) {
                obj.creation_date = dirInfo.CreationTimeUtc;
            }

            //
            // file_count
            if (fields.Exists("file_count")) {
                obj.file_count = dirInfo.GetFiles().Length;
            }

            //
            // directory_count
            if (fields.Exists("directory_count")) {
                obj.directory_count = dirInfo.GetDirectories().Length;
            }

            //
            // parent
            if (fields.Exists("parent")) {
                if (path == "/") {
                    obj.parent = null;
                }
                else if (Directory.Exists(PathUtil.RemoveLastSegment(physicalPath))) {
                    obj.parent = DirectoryToJsonModelRef(site, PathUtil.RemoveLastSegment(path));
                }
            }

            //
            // site
            if (fields.Exists("site")) {
                obj.site = SiteHelper.ToJsonModelRef(site);
            }

            return Core.Environment.Hal.Apply(Defines.FilesResource.Guid, obj, full);
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

        public static object VirtualDirectoryToJsonModel(Site site, string path, Fields fields = null, bool full = true)
        {
            if (string.IsNullOrEmpty(path)) {
                throw new ArgumentNullException(nameof(path));
            }

            if (fields == null) {
                fields = Fields.All;
            }

            var physicalPath = GetPhysicalPath(site, path);

            dynamic obj = new ExpandoObject();
            var FileId = new FileId(site.Id, path);
            DirectoryInfo dirInfo = new DirectoryInfo(physicalPath);
            var app = ResolveApplication(site, path);
            var vdir = ResolveVdir(site, path);

            //
            // name
            if (fields.Exists("name")) {
                obj.name = path == "/" ? site.Name : path.Replace('\\', '/').TrimStart('/');
            }

            //
            // id
            if (fields.Exists("id")) {
                obj.id = FileId.Uuid;
            }

            //
            // type
            if (fields.Exists("type")) {
                obj.type = Enum.GetName(typeof(FileType), FileType.VirtualDirectory).ToLower();
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
            // last_modified
            if (fields.Exists("last_modified")) {
                obj.last_modified = dirInfo.LastWriteTimeUtc;
            }

            //
            // creation_date
            if (fields.Exists("creation_date")) {
                obj.creation_date = dirInfo.CreationTimeUtc;
            }

            //
            // file_count
            if (fields.Exists("file_count")) {
                obj.file_count = dirInfo.GetFiles().Length;
            }

            //
            // directory_count
            if (fields.Exists("directory_count")) {
                obj.directory_count = dirInfo.GetDirectories().Length;
            }

            //
            // parent
            if (fields.Exists("parent")) {
                if (path == "/") {
                    obj.parent = null;
                }
                else if (vdir.Path == "/") {
                    obj.parent = DirectoryToJsonModelRef(site, vdir.Path);
                }
                else {
                    obj.parent = VirtualDirectoryToJsonModelRef(site, app.Path);
                }
            }

            //
            // site
            if (fields.Exists("site")) {
                obj.site = SiteHelper.ToJsonModelRef(site);
            }

            return Core.Environment.Hal.Apply(Defines.FilesResource.Guid, obj, full);
        }

        public static object VirtualDirectoryToJsonModelRef(Site site, string path, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return VirtualDirectoryToJsonModel(site, path, DirectoryRefFields, false);
            }
            else {
                return VirtualDirectoryToJsonModel(site, path, fields, false);
            }
        }

        public static object VirtualDirectoryToJsonModel(FullyQualifiedVirtualDirectory fullVdir, Fields fields = null, bool full = true)
        {
            if (fullVdir == null) {
                return null;
            }

            if (fields == null) {
                fields = Fields.All;
            }

            var virtualPath = fullVdir.Application.Path.TrimEnd('/') + fullVdir.VirtualDirectory.Path;
            var physicalPath = GetPhysicalPath(fullVdir.Site, virtualPath);

            dynamic obj = new ExpandoObject();
            var FileId = new FileId(fullVdir.Site.Id, virtualPath);
            DirectoryInfo dirInfo = new DirectoryInfo(physicalPath);
            //var app = ResolveApplication(site, path);
            //var vdir = ResolveVdir(site, path);

            //
            // name
            if (fields.Exists("name")) {
                obj.name = virtualPath == "/" ? fullVdir.Site.Name : virtualPath.TrimStart('/');
            }

            //
            // id
            if (fields.Exists("id")) {
                obj.id = FileId.Uuid;
            }

            //
            // type
            if (fields.Exists("type")) {
                obj.type = Enum.GetName(typeof(FileType), FileType.VirtualDirectory).ToLower();
            }

            //
            // path
            if (fields.Exists("path")) {
                obj.path = virtualPath;
            }

            //
            // physical_path
            if (fields.Exists("physical_path")) {
                obj.physical_path = physicalPath;
            }

            //
            // last_modified
            if (fields.Exists("last_modified")) {
                obj.last_modified = dirInfo.LastWriteTimeUtc;
            }

            //
            // creation_date
            if (fields.Exists("creation_date")) {
                obj.creation_date = dirInfo.CreationTimeUtc;
            }

            //
            // file_count
            if (fields.Exists("file_count")) {
                obj.file_count = dirInfo.GetFiles().Length;
            }

            //
            // directory_count
            if (fields.Exists("directory_count")) {
                obj.directory_count = dirInfo.GetDirectories().Length;
            }

            //
            // parent
            if (fields.Exists("parent")) {
                if (fullVdir.Application.Path == "/") {
                    obj.parent = null;
                }
                else if (fullVdir.VirtualDirectory.Path == "/") {
                    obj.parent = DirectoryToJsonModelRef(fullVdir.Site, fullVdir.VirtualDirectory.Path);
                }
                else {
                    obj.parent = VirtualDirectoryToJsonModelRef(fullVdir);
                }
            }

            //
            // site
            if (fields.Exists("site")) {
                obj.site = SiteHelper.ToJsonModelRef(fullVdir.Site);
            }

            return Core.Environment.Hal.Apply(Defines.FilesResource.Guid, obj, full);
        }

        public static object VirtualDirectoryToJsonModelRef(FullyQualifiedVirtualDirectory vdir, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return VirtualDirectoryToJsonModel(vdir, DirectoryRefFields, false);
            }
            else {
                return VirtualDirectoryToJsonModel(vdir, fields, false);
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

            var physicalPath = GetPhysicalPath(site, path);

            dynamic obj = new ExpandoObject();
            var FileId = new FileId(site.Id, path);
            var fileInfo = new FileInfo(physicalPath);

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
            // last_modified
            if (fields.Exists("last_modified")) {
                obj.last_modified = fileInfo.LastWriteTimeUtc;
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
            // parent
            if (fields.Exists("parent")) {
                if (path == "/") {
                    obj.parent = null;
                }
                else if (Directory.Exists(PathUtil.RemoveLastSegment(physicalPath))) {
                    obj.parent = DirectoryToJsonModelRef(site, PathUtil.RemoveLastSegment(path));
                }
            }

            //
            // site
            if (fields.Exists("site")) {
                obj.site = SiteHelper.ToJsonModelRef(site);
            }


            return Core.Environment.Hal.Apply(Defines.FilesResource.Guid, obj, full);
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

        public static FileType GetFileType(Site site, string path, string physicalPath)
        {
            if (path == "/" || !Path.Combine(FilesHelper.GetPhysicalPath(site), path.Replace('/', '\\').TrimStart('\\')).Equals(physicalPath, StringComparison.OrdinalIgnoreCase)) {
                return FileType.VirtualDirectory;
            }
            else if (Directory.Exists(physicalPath)) {
                return FileType.Directory;
            }

            return FileType.File;
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
                foreach (var vdir in parentApp.VirtualDirectories) {
                    var matchingPrefix = PathUtil.PrefixSegments(vdir.Path, path);
                    if (matchingPrefix > maxMatch) {
                        parentVdir = vdir;
                        maxMatch = matchingPrefix;
                    }
                }
            }
            return parentVdir;
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

        public static string GetPhysicalPath(Application app)
        {
            if (app == null) {
                throw new ArgumentNullException(nameof(app));
            }

            string root = null;
            if (app != null && app.VirtualDirectories["/"] != null) {
                root = app.VirtualDirectories["/"].PhysicalPath;
            }
            return root;
        }

        public static string GetPhysicalPath(Site site, string path)
        {
            var app = ResolveApplication(site, path);
            var vdir = ResolveVdir(site, path);

            var suffix = path.TrimStart(PathUtil.SEPARATORS);
            suffix = suffix.Remove(0, app.Path.TrimStart(PathUtil.SEPARATORS).Length);
            suffix = suffix.Remove(0, vdir.Path.TrimStart(PathUtil.SEPARATORS).Length);

            return vdir.PhysicalPath == null ? null : Path.Combine(vdir.PhysicalPath, suffix.Trim(PathUtil.SEPARATORS).Replace('/', Path.DirectorySeparatorChar));
        }

        public static List<FullyQualifiedVirtualDirectory> GetChildVirtualDirectories(Site site, string path)
        {
            var vdirs = new List<FullyQualifiedVirtualDirectory>();

            if (path == "/") {
                foreach (var app in site.Applications) {
                    if (app.Path != "/") {
                        foreach (var vdir in app.VirtualDirectories) {
                            if (vdir.Path == "/") {
                                vdirs.Add(new FullyQualifiedVirtualDirectory(site, app, vdir));
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
                            vdirs.Add(new FullyQualifiedVirtualDirectory(site, app, vdir));
                        }
                    }
                }
            }
            return vdirs;
        }




        //private static IEnumerable<Application> GetChildApplications(Site site, string path)
        //{
        //    var apps = new List<Application>();

        //    foreach (var app in site.Applications) {
        //        if (PathUtil.IsParentPath(path, app.Path)) {
        //            apps.Add(app);
        //        }
        //    }

        //    return apps;
        //}

        //private static IEnumerable<Application> GetParentApplications(Site site, string path)
        //{
        //    var apps = new List<Application>();

        //    // Get all parent applications for the path
        //    foreach (var app in site.Applications) {
        //        if (PathUtil.IsParentPath(app.Path, path)) {
        //            apps.Add(app);
        //        }
        //    }

        //    return apps;
        //}

        //private static IEnumerable<VirtualDirectory> GetChildVirtualDirectories(Site site, string path)
        //{
        //    var vdirs = new List<VirtualDirectory>();
        //    var apps = GetParentApplications(site, path);

        //    // Get all virtual directories that are children of the path
        //    foreach (var app in apps) {
        //        foreach (var vdir in app.VirtualDirectories) {
        //            var fullPath = app.Path.TrimEnd('/') + vdir.Path;
        //            if (PathUtil.IsParentPath(path, fullPath)) {
        //                vdirs.Add(vdir);
        //            }
        //        }
        //    }

        //    return vdirs;
        //}
    }
}
