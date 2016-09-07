// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering
{
    using Core;
    using Core.Utils;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Web.Administration;

    public class ExtensionsHelper
    {
        public static List<Extension> GetExtensions(Site site, string path, string configPath = null)
        {
            // Get request filtering section
            RequestFilteringSection requestFilteringSection = RequestFilteringHelper.GetRequestFilteringSection(site, path, configPath);

            var collection = requestFilteringSection.FileExtensions;
            if (collection != null) {
                return collection.ToList();
            }
            return new List<Extension>();
        }

        public static Extension CreateExtension(dynamic model, RequestFilteringSection section)
        {
            if (model == null) {
                throw new ArgumentNullException("model");
            }

            string extension = DynamicHelper.Value(model.extension);
            if (string.IsNullOrEmpty(extension)) {
                throw new ApiArgumentException("extension");
            }

            Extension ext = section.FileExtensions.CreateElement();
            
            ext.FileExtension = extension.StartsWith(".") ? extension : "." + extension;

            ext.Allowed = DynamicHelper.To<bool>(model.allow) ?? ext.Allowed;

            return ext;
        }

        public static void AddExtension(Extension extension, RequestFilteringSection section)
        {
            if (extension == null) {
                throw new ArgumentNullException("extension");
            }
            if (extension.FileExtension == null) {
                throw new ArgumentNullException("extension.FileExtension");
            }

            var collection = section.FileExtensions;

            if (collection.Any(ext => ext.FileExtension.Equals(extension.FileExtension))) {
                throw new AlreadyExistsException("extension");
            }

            try {
                collection.Add(extension);
            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
        }

        public static Extension UpdateExtension(Extension extension, dynamic model)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (extension == null) {
                throw new ApiArgumentException("extension");
            }

            DynamicHelper.If((object)model.extension, ext => extension.FileExtension = ext.StartsWith(".") ? ext : "." + ext);
            extension.Allowed = DynamicHelper.To<bool>(model.allow) ?? extension.Allowed;

            return extension;
        }

        public static void DeleteExtension(Extension extension, RequestFilteringSection section)
        {
            if (extension == null) {
                return;
            }

            var collection = section.FileExtensions;

            extension = collection.FirstOrDefault(e => e.FileExtension.Equals(extension.FileExtension));

            if (extension != null) {
                try {
                    collection.Remove(extension);
                }
                catch (FileLoadException e) {
                    throw new LockedException(section.SectionPath, e);
                }
                catch (DirectoryNotFoundException e) {
                    throw new ConfigScopeNotFoundException(e);
                }
            }
        }

        internal static object ToJsonModel(Extension extension, Site site, string path)
        {
            if (extension == null) {
                return null;
            }
            ExtensionId extensionId = new ExtensionId(site?.Id, path, extension.FileExtension);

            var obj = new {
                extension = extension.FileExtension.TrimStart(new char[] { '.' }),
                id = extensionId.Uuid,
                allow = extension.Allowed,
                request_filtering = RequestFilteringHelper.ToJsonModelRef(site, path)
            };

            return Core.Environment.Hal.Apply(Defines.FileExtensionsResource.Guid, obj);
        }

        public static object ToJsonModelRef(Extension extension, Site site, string path)
        {
            if (extension == null) {
                return null;
            }
            ExtensionId extensionId = new ExtensionId(site?.Id, path, extension.FileExtension);

            var obj = new {
                extension = extension.FileExtension.TrimStart(new char[] { '.' }),
                id = extensionId.Uuid,
                allow = extension.Allowed
            };

            return Core.Environment.Hal.Apply(Defines.FileExtensionsResource.Guid, obj, false);
        }

        public static string GetLocation(string id) {
            return $"/{Defines.FILE_NAME_EXTENSIONS_PATH}/{id}";
        }
    }
}
