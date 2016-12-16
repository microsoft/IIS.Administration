// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using Core;
    using Core.Utils;
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO;

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
                obj.exists = info.Exists;
            }

            //
            // created
            if (fields.Exists("created")) {
                obj.created = info.Exists ? (object)info.CreationTimeUtc : null;
            }

            //
            // last_modified
            if (fields.Exists("last_modified")) {
                obj.last_modified = info.Exists ? (object)info.LastWriteTimeUtc : null;
            }

            //
            // total_files
            if (fields.Exists("total_files")) {
                if (_service.IsAccessAllowed(info.FullName, FileAccess.Read)) {
                    obj.total_files = info.Exists ? info.GetFiles().Length + info.GetDirectories().Length : 0;
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
                obj.exists = info.Exists;
            }

            //
            // size
            if (fields.Exists("size")) {
                obj.size = info.Exists ? info.Length : 0;
            }

            //
            // created
            if (fields.Exists("created")) {
                obj.created = info.Exists ? (object)info.CreationTimeUtc : null;
            }

            //
            // last_modified
            if (fields.Exists("last_modified")) {
                obj.last_modified = info.Exists ? (object)info.LastWriteTimeUtc : null;
            }

            //
            // e_tag
            if (fields.Exists("e_tag")) {
                obj.e_tag = info.Exists ? ETag.Create(info).Value : null;
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
                obj.exists = info.Exists;
            }

            //
            // created
            if (fields.Exists("created")) {
                obj.created = info.Exists ? (object)info.CreationTimeUtc : null;
            }

            //
            // last_modified
            if (fields.Exists("last_modified")) {
                obj.last_modified = info.Exists ? (object)info.LastWriteTimeUtc : null;
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
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            string name = DynamicHelper.Value(model.name);

            if (name != null) {

                if (!PathUtil.IsValidFileName(name)) {
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

        internal static string UpdateDirectory(dynamic model, string directoryPath)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            string name = DynamicHelper.Value(model.name);

            if (name != null) {

                if (!PathUtil.IsValidFileName(name)) {
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

        internal static FileType GetFileType(string physicalPath)
        {
            if (Directory.Exists(physicalPath)) {
                return FileType.Directory;
            }

            if (File.Exists(physicalPath)) {
                return FileType.File;
            }

            throw new FileNotFoundException();
        }

        private static object GetParentJsonModelRef(string physicalPath, Fields fields = null)
        {
            object ret = null;

            var parentPath = _service.GetParentPath(physicalPath);

            if (parentPath != null && _service.IsAccessAllowed(parentPath, FileAccess.Read)) {
                try {
                    ret = GetFileType(parentPath) == FileType.File ? FileToJsonModelRef(new FileInfo(parentPath), fields)
                                                                        : DirectoryToJsonModelRef(new DirectoryInfo(parentPath), fields);
                }
                catch (FileNotFoundException) {
                    ret = InfoToJsonModel(new FileInfo(parentPath), fields);
                }
            }

            return ret;
        }
    }
}
