// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
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
            var fileType = GetFileType(physicalPath);

            switch (fileType) {

                case FileType.File:
                    return FileToJsonModel(_service.GetFileInfo(physicalPath), fields, full);

                case FileType.Directory:
                    return DirectoryToJsonModel(_service.GetDirectoryInfo(physicalPath), fields, full);

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

        internal static object DirectoryToJsonModel(DirectoryInfo info, Fields fields = null, bool full = true)
        {
            if (fields == null) {
                fields = Fields.All;
            }

            if (!info.Exists) {
                return null;
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
            // permissions
            if (fields.Exists("permission")) {
                obj.permissions = GetPermissions(info.FullName);
            }

            //
            // created
            if (fields.Exists("created")) {
                obj.created = info.CreationTimeUtc;
            }

            //
            // last_modified
            if (fields.Exists("last_modified")) {
                obj.last_modified = info.LastWriteTimeUtc;
            }

            //
            // total_files
            if (fields.Exists("total_files")) {
                if (_service.IsAccessAllowed(info.FullName, FileAccess.Read)) {
                    obj.total_files = info.GetFiles().Length + info.GetDirectories().Length;
                }
            }

            //
            // parent
            if (fields.Exists("parent")) {
                obj.parent = GetParentJsonModelRef(info.FullName);
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

            if (!info.Exists) {
                return null;
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
            // permissions
            if (fields.Exists("permission")) {
                obj.permissions = GetPermissions(info.FullName);
            }

            //
            // size
            if (fields.Exists("size")) {
                obj.size = info.Length;
            }

            //
            // created
            if (fields.Exists("created")) {
                obj.created = info.CreationTimeUtc;
            }

            //
            // last_modified
            if (fields.Exists("last_modified")) {
                obj.last_modified = info.LastWriteTimeUtc;
            }

            //
            // e_tag
            if (fields.Exists("e_tag")) {
                obj.e_tag = ETag.Create(info).Value;
            }

            //
            // parent
            if (fields.Exists("parent")) {
                obj.parent = GetParentJsonModelRef(info.FullName);
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



        internal static string GetPhysicalPath(string root, string path)
        {
            if (string.IsNullOrEmpty(root)) {
                throw new ArgumentNullException(nameof(root));
            }

            return PathUtil.GetFullPath(Path.Combine(root, path.TrimStart(PathUtil.SEPARATORS)));
        }

        private static object GetParentJsonModelRef(string physicalPath)
        {
            object ret = null;

            var parentPath = _service.GetParentPath(physicalPath);

            if (parentPath != null && _service.IsAccessAllowed(parentPath, FileAccess.Read)) {
                ret = GetFileType(parentPath) == FileType.File ? FileToJsonModelRef(_service.GetFileInfo(parentPath))
                                                                    : DirectoryToJsonModelRef(_service.GetDirectoryInfo(parentPath));
            }

            return ret;
        }

        private static FileType GetFileType(string physicalPath)
        {
            if (_service.DirectoryExists(physicalPath)) {
                return FileType.Directory;
            }

            if (_service.FileExists(physicalPath)) {
                return FileType.File;
            }

            throw new FileNotFoundException();
        }

        private static IEnumerable<string> GetPermissions(string physicalPath)
        {
            List<string> permissions = new List<string>();
            var allowedAccess = _acessControl.GetFileAccess(physicalPath);

            // Manually add flags to avoid the ReadWrite flag being added
            if (allowedAccess.HasFlag(FileAccess.Read)) {
                permissions.Add("read");
            }
            if (allowedAccess.HasFlag(FileAccess.Write)) {
                permissions.Add("write");
            }

            return permissions;
        }
    }
}
