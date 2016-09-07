// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.DefaultDocuments
{
    using Core;
    using Core.Utils;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Web.Administration;

    public class FilesHelper
    {
        public static List<File> GetFiles(Site site, string path)
        {
            return DefaultDocumentHelper.GetDefaultDocumentSection(site, path).Files.ToList();
        }

        internal static object ToJsonModel(File file, Site site, string path)
        {
            if (file == null) {
                return null;
            }

            // FileId for uniquely identifying the file
            FileId fileId = new FileId(site?.Id, path, file.Name);

            var obj = new {
                name = file.Name,
                id = fileId.Uuid,
                default_document = DefaultDocumentHelper.ToJsonModelRef(site, path)
            };

            return Core.Environment.Hal.Apply(Defines.FilesResource.Guid, obj);
        }

        public static object ToJsonModelRef(File file, Site site, string path)
        {
            if (file == null) {
                return null;
            }

            // Construct id passing with possible site and application associated
            FileId fileId = new FileId(site?.Id, path, file.Name);

            var obj = new {
                name = file.Name,
                id = fileId.Uuid,
            };

            return Core.Environment.Hal.Apply(Defines.FilesResource.Guid, obj, false);
        }

        public static File CreateFile(dynamic model, DefaultDocumentSection section)
        {
            // Ensure integrity of model
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (section == null) {
                throw new ArgumentNullException("section");
            }

            string name = DynamicHelper.Value(model.name);
            if (String.IsNullOrEmpty(name)) {
                throw new ApiArgumentException("name");
            }

            File file = section.Files.CreateElement();
            file.Name = name;

            return file;
        }

        public static File AddFile(File file, DefaultDocumentSection section)
        {
            if (section == null) {
                throw new ArgumentNullException("section");
            }

            // Try to get default document with name specified to ensure it doesn't exist
            File element = section.Files[file.Name];
            if (element != null) {
                throw new ApiArgumentException("Default Document already exists.", "document");
            }

            try {
                element = section.Files.AddAt(0, file.Name);
            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }

            return file;
        }

        public static File UpdateFile(File file, dynamic model, DefaultDocumentSection section)
        {
            if (model == null) {
                throw new ArgumentNullException("model");
            }

            string name = DynamicHelper.Value(model.name);

            if (!string.IsNullOrEmpty(name)) {

                if(!name.Equals(file.Name, StringComparison.OrdinalIgnoreCase)
                    && section.Files.Any(f => name.Equals(f.Name, StringComparison.OrdinalIgnoreCase))) {
                    throw new AlreadyExistsException("name");
                }

                file.Name = name;
            }

            return file;
        }

        public static void DeleteFile(File document, DefaultDocumentSection section)
        {

            if (document == null) {
                throw new ArgumentNullException("document");
            }

            File element = section.Files[document.Name];
            if (element == null) {
                return;
            }

            try {
                // Delete the document
                section.Files.Remove(element);
            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
        }

        public static string GetLocation(string id) {
            return $"/{Defines.FILES_PATH}/{id}";
        }
    }
}
