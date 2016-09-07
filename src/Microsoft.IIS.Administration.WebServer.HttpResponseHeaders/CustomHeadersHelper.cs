// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpResponseHeaders
{
    using Core;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Web.Administration;
    using Core.Utils;


    public static class CustomHeadersHelper
    {
        public static List<NameValueConfigurationElement> GetCustomHeaders(Site site, string path)
        {
            return HttpResponseHeadersHelper.GetSection(site, path).CustomHeaders.ToList();
        }

        public static NameValueConfigurationElement Create(dynamic model, HttpProtocolSection section)
        {
            if (section == null) {
                throw new ArgumentException("section");
            }
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            string name = DynamicHelper.Value(model.name);
            string value = DynamicHelper.Value(model.value);

            if (String.IsNullOrEmpty(name)) {
                throw new ApiArgumentException("name");
            }
            if (value == null) {
                // We don't check for empty because there is nothing wrong with an empty string header value

                throw new ApiArgumentException("value");
            }

            var elem = section.CustomHeaders.CreateElement();

            elem.Name = name;
            elem.Value = value;

            return elem;
        }

        public static void Add(NameValueConfigurationElement header, HttpProtocolSection section)
        {
            if(header == null) {
                throw new ArgumentNullException("header");
            }
            if (section == null) {
                throw new ArgumentNullException("section");
            }

            if(section.CustomHeaders.FirstOrDefault(h => h.Name.Equals(header.Name, StringComparison.OrdinalIgnoreCase)) != null) {
                throw new AlreadyExistsException("header");
            }

            try {
                section.CustomHeaders.Add(header);
            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
        }

        public static void Update(NameValueConfigurationElement header, dynamic model, HttpProtocolSection section)
        {
            if (header == null) {
                throw new ArgumentNullException("header");
            }
            if (section == null) {
                throw new ArgumentNullException("section");
            }
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            try {

                string name = DynamicHelper.Value(model.name);
                if (!string.IsNullOrEmpty(name)) {

                    if (section.CustomHeaders.Any(h => h.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) {
                        throw new AlreadyExistsException("header.name");
                    }

                    header.Name = name;
                }

                header.Value = DynamicHelper.Value(model.value) ?? header.Value;

            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }        
        }

        public static void Delete(NameValueConfigurationElement header, HttpProtocolSection section)
        {
            if(section == null) {
                throw new ArgumentNullException("section");
            }

            if(header == null) {
                return;
            }

            // Need to pull the element out of the collection we are removing it from
            header = section.CustomHeaders.FirstOrDefault(h => h.Name.Equals(header.Name, StringComparison.OrdinalIgnoreCase));

            if(header != null) {

                try {
                    section.CustomHeaders.Remove(header);
                }
                catch (FileLoadException e) {
                    throw new LockedException(section.SectionPath, e);
                }
                catch (DirectoryNotFoundException e) {
                    throw new ConfigScopeNotFoundException(e);
                }
            }
        }

        internal static object ToJsonModel(NameValueConfigurationElement header, Site site, string path)
        {
            if (header == null) {
                return null;
            }

            CustomHeaderId id = new CustomHeaderId(site?.Id, path, header.Name);

            var obj = new {
                name = header.Name,
                value = header.Value,
                id = id.Uuid,
                http_response_headers = HttpResponseHeadersHelper.ToJsonModelRef(site, path)
            };

            return Core.Environment.Hal.Apply(Defines.CustomHeadersResource.Guid, obj);
        }

        public static object ToJsonModelRef(NameValueConfigurationElement header, Site site, string path)
        {
            if (header == null) {
                return null;
            }

            CustomHeaderId id = new CustomHeaderId(site?.Id, path, header.Name);

            var obj = new {
                name = header.Name,
                value = header.Value,
                id = id.Uuid
            };

            return Core.Environment.Hal.Apply(Defines.CustomHeadersResource.Guid, obj, false);
        }

        public static string GetLocation(string id) {
            return $"/{Defines.CUSTOM_HEADERS_PATH}/{id}";
        }
    }
}
