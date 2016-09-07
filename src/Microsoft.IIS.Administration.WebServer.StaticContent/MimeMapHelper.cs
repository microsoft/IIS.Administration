// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.StaticContent
{
    using Core;
    using Web.Administration;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Core.Utils;
    using System.Dynamic;


    public static class MimeMapHelper
    {
        private static readonly Fields RefFields = new Fields("file_extension", "id", "mime_type");

        public static MimeMap CreateMimeMap(dynamic model, StaticContentSection section)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            string fileExtension = DynamicHelper.Value(model.file_extension);
            string mimeType = DynamicHelper.Value(model.mime_type);

            if (string.IsNullOrEmpty(fileExtension)) {
                throw new ApiArgumentException("file_extension");
            }
            if (string.IsNullOrEmpty(mimeType)) {
                throw new ApiArgumentException("mime_type");
            }

            MimeMap mimeMap = section.MimeMaps.CreateElement();
            mimeMap.FileExtension = fileExtension.StartsWith(".") ? fileExtension : "." + fileExtension;
            mimeMap.MimeType = mimeType;

            return mimeMap;
        }

        public static void AddMimeMap(MimeMap mimeMap, StaticContentSection section) {
            if (mimeMap == null) {
                throw new ArgumentNullException("mimeMap");
            }

            MimeMapCollection collection = section.MimeMaps;
            if (collection[mimeMap.FileExtension] != null) {
                throw new AlreadyExistsException("file_extension");
            }

            try {
                collection.Add(mimeMap);
            }
            catch(FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
        }

        public static MimeMap UpdateMimeMap(dynamic model, MimeMap mimeMap, StaticContentSection section)
        {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }

            if (mimeMap == null) {
                throw new ArgumentNullException(nameof(mimeMap));
            }

            if (string.IsNullOrEmpty(mimeMap.FileExtension)) {
                throw new ArgumentNullException("mimeMap.FileExtension");
            }

            string fileExtension = DynamicHelper.Value(model.file_extension);
            string mimeType = DynamicHelper.Value(model.mime_type);

            if (!string.IsNullOrEmpty(fileExtension)) {

                fileExtension = fileExtension.StartsWith(".") ? fileExtension : "." + fileExtension;

                if (!fileExtension.Equals(mimeMap.FileExtension, StringComparison.OrdinalIgnoreCase)
                        && section.MimeMaps.Any(m => fileExtension.Equals(m.FileExtension, StringComparison.OrdinalIgnoreCase))) {
                    throw new AlreadyExistsException("file_extension");
                }
            }

            try {
                mimeMap.FileExtension = fileExtension ?? mimeMap.FileExtension;
                mimeMap.MimeType = mimeType ?? mimeMap.MimeType;
            }
            catch (FileLoadException e) {
                throw new LockedException(MimeTypesGlobals.StaticContentSectionName, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }

            return mimeMap;
        }

        public static void DeleteMimeType(MimeMap mimeMap, StaticContentSection section) {
            if (mimeMap == null) {
                throw new ArgumentNullException("mimeMap");
            }

            MimeMapCollection collection = section.MimeMaps;

            MimeMap element = collection[mimeMap.FileExtension];
            if (element == null) {
                return;
            }

            try {
                collection.Remove(mimeMap.FileExtension);
            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
        }

        public static List<MimeMap> GetMimeMaps(Site site, string path, string configPath = null) {

            var collection = StaticContentHelper.GetSection(site, path, configPath).MimeMaps;

            return collection.ToList();
        }

        internal static object ToJsonModel(MimeMap mimeMap, Site site, string path, Fields fields = null, bool full = true)
        {
            if (mimeMap == null) {
                return null;
            }

            if (fields == null)
            {
                fields = Fields.All;
            }

            dynamic obj = new ExpandoObject();

            //
            // file_extension
            if (fields.Exists("file_extension"))
            {
                obj.file_extension = mimeMap.FileExtension.TrimStart(new char[] { '.' });
            }

            //
            // id
            obj.id = new MimeMapId(site?.Id, path, mimeMap.FileExtension).Uuid;

            //
            // mime_type
            if (fields.Exists("mime_type"))
            {
                obj.mime_type = mimeMap.MimeType;
            }

            //
            // static_content
            if (fields.Exists("static_content"))
            {
                obj.static_content = StaticContentHelper.ToJsonModelRef(site, path);
            }

            return Core.Environment.Hal.Apply(Defines.MimeMapsResource.Guid, obj, full);

        }

        public static object ToJsonModelRef(MimeMap mimeMap, Site site, string path, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return ToJsonModel(mimeMap, site, path, RefFields, false);
            }
            else {
                return ToJsonModel(mimeMap, site, path, fields, false);
            }
        }

        public static string GetLocation(string id)
        {
            return $"/{Defines.MIME_MAPS_PATH}/{id}";
        }
    }
}
