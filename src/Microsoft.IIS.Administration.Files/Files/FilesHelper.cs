// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using Core;
    using Core.Utils;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Dynamic;
    using System.IO;
    using System.Linq;

    public static class FilesHelper
    {
        private static readonly Fields RefFields = new Fields("name", "id", "type", "physical_path");

        private static IFileProvider _service = FileProvider.Default;
        private static IAccessControl _acessControl = AccessControl.Default;

        public static object ToJsonModel(string physicalPath, Fields fields = null, bool full = true)
        {
            FileType fileType;

            try {
                fileType = GetFileType(physicalPath);
            }
            catch (FileNotFoundException) {
                return InfoToJsonModel(new FileInfo(physicalPath), fields, full);
            }

            switch (fileType) {

                case FileType.File:
                    return FileToJsonModel(new FileInfo(physicalPath), fields, full);

                case FileType.Directory:
                    return DirectoryToJsonModel(new DirectoryInfo(physicalPath), fields, full);

                default:
                    return null;
            }
        }

        public static object ToJsonModelRef(string physicalPath, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return ToJsonModel(physicalPath, RefFields, false);
            }
            else {
                return ToJsonModel(physicalPath, fields, false);
            }
        }

        public static string GetLocation(string id)
        {
            return $"/{Defines.FILES_PATH}/{id}";
        }

        internal static object DirectoryToJsonModel(DirectoryInfo info, Fields fields = null, bool full = true)
        {
            if (fields == null) {
                fields = Fields.All;
            }

            dynamic obj = new ExpandoObject();
            var fileId = FileId.FromPhysicalPath(info.FullName);
            bool? exists = null;

            //
            // name
            if (fields.Exists("name")) {
                obj.name = info.Name;
            }

            //
            // id
            if (fields.Exists("id")) {
                obj.id = fileId.Uuid;
            }

            //
            // type
            if (fields.Exists("type")) {
                obj.type = Enum.GetName(typeof(FileType), FileType.Directory).ToLower();
            }

            //
            // physical_path
            if (fields.Exists("physical_path")) {
                obj.physical_path = info.FullName;
            }

            //
            // exists
            if (fields.Exists("exists")) {
                exists = exists ?? info.Exists;
                obj.exists = exists.Value;
            }

            //
            // created
            if (fields.Exists("created")) {
                exists = exists ?? info.Exists;
                obj.created = exists.Value ? (object)info.CreationTimeUtc : null;
            }

            //
            // last_modified
            if (fields.Exists("last_modified")) {
                exists = exists ?? info.Exists;
                obj.last_modified = exists.Value ? (object)info.LastWriteTimeUtc : null;
            }

            //
            // last_access
            if (fields.Exists("last_access")) {
                exists = exists ?? info.Exists;
                obj.last_access = exists.Value ? (object)info.LastAccessTimeUtc : null;
            }

            //
            // total_files
            // We check for the 'full' flag to avoid unauthorized exception when referencing directories
            // Listing a directories content requires extra permissions
            if (fields.Exists("total_files") && full) {
                exists = exists ?? info.Exists;
                if (_service.IsAccessAllowed(info.FullName, FileAccess.Read)) {
                    obj.total_files = exists.Value ? _service.GetFiles(info.FullName, "*").Count() + _service.GetDirectories(info.FullName, "*").Count() : 0;
                }
            }

            //
            // parent
            if (fields.Exists("parent")) {
                obj.parent = GetParentJsonModelRef(info.FullName, fields.Filter("parent"));
            }

            //
            // claims
            if (fields.Exists("claims")) {
                obj.claims = _acessControl.GetClaims(info.FullName);
            }

            return Core.Environment.Hal.Apply(Defines.DirectoriesResource.Guid, obj, full);
        }

        internal static object DirectoryToJsonModelRef(DirectoryInfo info, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return DirectoryToJsonModel(info, RefFields, false);
            }
            else {
                return DirectoryToJsonModel(info, fields, false);
            }
        }

        internal static object FileToJsonModel(FileInfo info, Fields fields = null, bool full = true)
        {
            if (fields == null) {
                fields = Fields.All;
            }

            dynamic obj = new ExpandoObject();
            var fileId = FileId.FromPhysicalPath(info.FullName);
            bool? exists = null;
            
            //
            // name
            if (fields.Exists("name")) {
                obj.name = info.Name;
            }

            //
            // id
            if (fields.Exists("id")) {
                obj.id = fileId.Uuid;
            }

            //
            // type
            if (fields.Exists("type")) {
                obj.type = Enum.GetName(typeof(FileType), FileType.File).ToLower();
            }

            //
            // physical_path
            if (fields.Exists("physical_path")) {
                obj.physical_path = info.FullName;
            }

            //
            // exists
            if (fields.Exists("exists")) {
                exists = exists ?? info.Exists;
                obj.exists = exists.Value;
            }

            //
            // size
            if (fields.Exists("size")) {
                exists = exists ?? info.Exists;
                obj.size = exists.Value ? info.Length : 0;
            }

            //
            // created
            if (fields.Exists("created")) {
                exists = exists ?? info.Exists;
                obj.created = exists.Value ? (object)info.CreationTimeUtc : null;
            }

            //
            // last_modified
            if (fields.Exists("last_modified")) {
                exists = exists ?? info.Exists;
                obj.last_modified = exists.Value ? (object)info.LastWriteTimeUtc : null;
            }

            //
            // last_access
            if (fields.Exists("last_access")) {
                exists = exists ?? info.Exists;
                obj.last_access = exists.Value ? (object)info.LastAccessTimeUtc : null;
            }

            //
            // e_tag
            if (fields.Exists("e_tag")) {
                exists = exists ?? info.Exists;
                obj.e_tag = exists.Value ? ETag.Create(info).Value : null;
            }

            //
            // parent
            if (fields.Exists("parent")) {
                obj.parent = GetParentJsonModelRef(info.FullName, fields.Filter("parent"));
            }

            //
            // claims
            if (fields.Exists("claims")) {
                obj.claims = _acessControl.GetClaims(info.FullName);
            }


            return Core.Environment.Hal.Apply(Defines.FilesResource.Guid, obj, full);
        }

        internal static object FileToJsonModelRef(FileInfo info, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return FileToJsonModel(info, RefFields, false);
            }
            else {
                return FileToJsonModel(info, fields, false);
            }
        }

        internal static object InfoToJsonModel(FileSystemInfo info, Fields fields = null, bool full = true)
        {
            if (fields == null) {
                fields = Fields.All;
            }

            dynamic obj = new ExpandoObject();
            var fileId = FileId.FromPhysicalPath(info.FullName);
            bool? exists = null;

            //
            // name
            if (fields.Exists("name")) {
                obj.name = info.Name;
            }

            //
            // id
            if (fields.Exists("id")) {
                obj.id = fileId.Uuid;
            }

            //
            // type
            if (fields.Exists("type")) {
                obj.type = null;
            }

            //
            // physical_path
            if (fields.Exists("physical_path")) {
                obj.physical_path = info.FullName;
            }

            //
            // exists
            if (fields.Exists("exists")) {
                exists = exists ?? info.Exists;
                obj.exists = exists.Value;
            }

            //
            // created
            if (fields.Exists("created")) {
                exists = exists ?? info.Exists;
                obj.created = exists.Value ? (object)info.CreationTimeUtc : null;
            }

            //
            // last_modified
            if (fields.Exists("last_modified")) {
                exists = exists ?? info.Exists;
                obj.last_modified = exists.Value ? (object)info.LastWriteTimeUtc : null;
            }

            //
            // last_access
            if (fields.Exists("last_access")) {
                exists = exists ?? info.Exists;
                obj.last_access = exists.Value ? (object)info.LastAccessTimeUtc : null;
            }

            //
            // parent
            if (fields.Exists("parent")) {
                obj.parent = GetParentJsonModelRef(info.FullName, fields.Filter("parent"));
            }

            //
            // claims
            if (fields.Exists("claims")) {
                obj.claims = _acessControl.GetClaims(info.FullName);
            }


            return Core.Environment.Hal.Apply(Defines.FilesResource.Guid, obj, full);
        }

        internal static object InfoToJsonModelRef(FileSystemInfo info, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return InfoToJsonModel(info, RefFields, false);
            }
            else {
                return InfoToJsonModel(info, fields, false);
            }
        }

        internal static string GetPhysicalPath(string root, string path)
        {
            if (string.IsNullOrEmpty(root)) {
                throw new ArgumentNullException(nameof(root));
            }

            return PathUtil.GetFullPath(Path.Combine(root, path.TrimStart(PathUtil.SEPARATORS)));
        }

        internal static string UpdateFile(dynamic model, string physicalPath)
        {
            string newName = null;
            string newParentPath = null;

            DateTime? created = null;
            DateTime? lastAccess = null;
            DateTime? lastModified = null;

            var file = new FileInfo(physicalPath);

            if (model == null) {
                throw new ApiArgumentException("model");
            }

            if (model.parent != null) {
                if (!(model.parent is JObject)) {
                    throw new ApiArgumentException("parent", ApiArgumentException.EXPECTED_OBJECT);
                }

                string parentUuid = DynamicHelper.Value(model.parent.id);

                if (string.IsNullOrEmpty(parentUuid)) {
                    throw new ApiArgumentException("parent.id");
                }

                string path = FileId.FromUuid(parentUuid).PhysicalPath;

                if (!PathUtil.IsFullPath(path)) {
                    throw new ApiArgumentException("parent.id");
                }

                if (!_service.DirectoryExists(path)) {
                    throw new NotFoundException("parent");
                }

                newParentPath = path;
            }

            if (model.name != null) {
                string name = DynamicHelper.Value(model.name);

                if (!PathUtil.IsValidFileName(name)) {
                    throw new ApiArgumentException("name");
                }

                newName = name;
            }

            created = DynamicHelper.To<DateTime>(model.created);
            lastAccess = DynamicHelper.To<DateTime>(model.last_access);
            lastModified = DynamicHelper.To<DateTime>(model.last_modified);

            if (newParentPath != null || newName != null) {

                newParentPath = newParentPath == null ? file.Directory.FullName : newParentPath;
                newName = newName == null ? file.Name : newName;

                var newPath = Path.Combine(newParentPath, newName);

                if (!newPath.Equals(physicalPath, StringComparison.OrdinalIgnoreCase)) {

                    if (_service.FileExists(newPath) || _service.DirectoryExists(newPath)) {
                        throw new AlreadyExistsException("name");
                    }

                    _service.MoveFile(physicalPath, newPath);

                    physicalPath = newPath;
                }
            }

            if (created.HasValue) {
                _service.SetCreationTime(physicalPath, created.Value);
            }

            if (lastAccess.HasValue) {
                _service.SetLastAccessTime(physicalPath, lastAccess.Value);
            }

            if (lastModified.HasValue) {
                _service.SetLastWriteTime(physicalPath, lastModified.Value);
            }

            return physicalPath;
        }

        internal static string UpdateDirectory(dynamic model, string directoryPath)
        {
            string newName = null;
            string newParentPath = null;

            DateTime? created = null;
            DateTime? lastAccess = null;
            DateTime? lastModified = null;

            var directory = new DirectoryInfo(directoryPath);

            if (model == null) {
                throw new ApiArgumentException("model");
            }

            if (model.parent != null) {
                if (!(model.parent is JObject)) {
                    throw new ApiArgumentException("parent", ApiArgumentException.EXPECTED_OBJECT);
                }

                if (directory.Parent == null) {
                    throw new ApiArgumentException("parent");
                }

                string parentUuid = DynamicHelper.Value(model.parent.id);

                if (string.IsNullOrEmpty(parentUuid)) {
                    throw new ApiArgumentException("parent.id");
                }

                string path = FileId.FromUuid(parentUuid).PhysicalPath;

                if (!PathUtil.IsFullPath(path)) {
                    throw new ApiArgumentException("parent.id");
                }

                if (!_service.DirectoryExists(path)) {
                    throw new NotFoundException("parent");
                }

                newParentPath = path;
            }

            if (model.name != null) {
                string name = DynamicHelper.Value(model.name);

                if (!PathUtil.IsValidFileName(name)) {
                    throw new ApiArgumentException("name");
                }

                newName = name;
            }

            created = DynamicHelper.To<DateTime>(model.created);
            lastAccess = DynamicHelper.To<DateTime>(model.last_access);
            lastModified = DynamicHelper.To<DateTime>(model.last_modified);

            if (newParentPath != null || newName != null) {

                newParentPath = newParentPath == null ? directory.Parent.FullName : newParentPath;
                newName = newName == null ? directory.Name : newName;

                var newPath = Path.Combine(newParentPath, newName);

                if (!newPath.Equals(directoryPath, StringComparison.OrdinalIgnoreCase)) {

                    if (_service.FileExists(newPath) || _service.DirectoryExists(newPath)) {
                        throw new AlreadyExistsException("name");
                    }

                    _service.MoveDirectory(directoryPath, newPath);

                    directoryPath = newPath;
                }
            }

            if (created.HasValue) {
                _service.SetCreationTime(directoryPath, created.Value);
            }

            if (lastAccess.HasValue) {
                _service.SetLastAccessTime(directoryPath, lastAccess.Value);
            }

            if (lastModified.HasValue) {
                _service.SetLastWriteTime(directoryPath, lastModified.Value);
            }

            return directoryPath;
        }

        internal static FileType GetFileType(string physicalPath)
        {
            return File.GetAttributes(physicalPath).HasFlag(FileAttributes.Directory) ? FileType.Directory : FileType.File;
        }

        private static object GetParentJsonModelRef(string physicalPath, Fields fields = null)
        {
            object ret = null;

            var parentPath = _service.GetParentPath(physicalPath);

            if (parentPath != null && _service.IsAccessAllowed(parentPath, FileAccess.Read)) {
                ret = DirectoryToJsonModelRef(new DirectoryInfo(parentPath), fields);
            }

            return ret;
        }
    }
}
