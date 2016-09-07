// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRequestTracing
{
    using Core;
    using Core.Utils;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO;
    using System.Linq;
    using Web.Administration;

    public static class ProvidersHelper
    {
        private static readonly Fields RefFields = new Fields("name", "id");

        public static IEnumerable<TraceProviderDefinition> GetProviders(Site site, string path, string configPath = null)
        {
            return Helper.GetTraceProviderDefinitionSection(site, path, configPath).TraceProviderDefinitions;
        }

        internal static object ToJsonModel(TraceProviderDefinition provider, Site site, string path, Fields fields = null, bool full = true)
        {
            if (provider == null) {
                return null;
            }

            if (fields == null) {
                fields = Fields.All;
            }

            dynamic obj = new ExpandoObject();

            //
            // name
            if (fields.Exists("name")) {
                obj.name = provider.Name;
            }

            //
            // id
            obj.id = new ProviderId(site?.Id, path, provider.Name).Uuid;

            //
            // guid
            if (fields.Exists("guid")) {
                obj.guid = provider.Guid.ToString("B");
            }

            //
            // areas
            if (fields.Exists("areas")) {
                obj.areas = provider.Areas.Select(area => area.Name);
            }

            //
            // request_tracing
            if (fields.Exists("request_tracing")) {
                obj.request_tracing = Helper.ToJsonModelRef(site, path);
            }

            return Core.Environment.Hal.Apply(Defines.ProvidersResource.Guid, obj, full);
        }

        public static TraceProviderDefinition CreateProvider(dynamic model, TraceProviderDefinitionsSection section)
        {
            //
            // model
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            //
            // name
            string name = DynamicHelper.Value(model.name);
            if (string.IsNullOrEmpty(name)) {
                throw new ApiArgumentException("name");
            }

            //
            // guid
            Guid guid;
            string g = DynamicHelper.Value(model.guid);
            if (!Guid.TryParse(g, out guid)) {
                throw new ApiArgumentException("guid");
            }

            //
            // areas
            if (model.areas == null) {
                throw new ApiArgumentException("areas");
            }

            var provider = section.TraceProviderDefinitions.CreateElement();

            SetProvider(provider, model);

            return provider;
        }

        public static object AddProvider(TraceProviderDefinition provider, TraceProviderDefinitionsSection section)
        {
            if (provider == null) {
                throw new ArgumentNullException("provider");
            }
            if (provider.Name == null) {
                throw new ArgumentNullException("provider.Name");
            }

            if (section.TraceProviderDefinitions.Any(prov => prov.Name.Equals(provider.Name, StringComparison.OrdinalIgnoreCase))) {
                throw new AlreadyExistsException("name");
            }

            if (section.TraceProviderDefinitions.Any(prov => prov.Guid.Equals(provider.Guid))) {
                throw new AlreadyExistsException("guid");
            }

            try {
                section.TraceProviderDefinitions.Add(provider);
            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }

            return provider;
        }

        public static TraceProviderDefinition UpdateProvider(TraceProviderDefinition provider, dynamic model, TraceProviderDefinitionsSection section)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (provider == null) {
                throw new ArgumentNullException("provider");
            }

            string name = DynamicHelper.Value(model.name);
            if (name != null 
                && !name.Equals(provider.Name)
                && section.TraceProviderDefinitions.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) {
                throw new AlreadyExistsException("name");
            }

            Guid guid;
            var g = DynamicHelper.Value(model.guid);
            if(Guid.TryParse(g, out guid)
                && !guid.Equals(provider.Guid)
                && section.TraceProviderDefinitions.Any(prov => prov.Guid.Equals(guid))) {
                throw new AlreadyExistsException("guid");
            }

            try {
                SetProvider(provider, model);
            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }

            return provider;

        }

        public static void DeleteProvider(TraceProviderDefinition provider, TraceProviderDefinitionsSection section)
        {
            if (provider == null) {
                return;
            }

            var collection = section.TraceProviderDefinitions;

            provider = collection.FirstOrDefault(p => p.Name.Equals(provider.Name));

            if (provider != null) {
                try {
                    collection.Remove(provider);
                }
                catch (FileLoadException e) {
                    throw new LockedException(section.SectionPath, e);
                }
                catch (DirectoryNotFoundException e) {
                    throw new ConfigScopeNotFoundException(e);
                }
            }
        }

        public static object ToJsonModelRef(TraceProviderDefinition provider, Site site, string path, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return ToJsonModel(provider, site, path, RefFields, false);
            }
            else {
                return ToJsonModel(provider, site, path, fields, false);
            }
        }

        public static string GetLocation(string id)
        {
            return $"/{Defines.PROVIDERS_PATH}/{id}";
        }

        private static void SetProvider(TraceProviderDefinition provider, dynamic model)
        {
            DynamicHelper.If((object)model.name, v => provider.Name = v);
            DynamicHelper.If((object)model.guid, v => {
                Guid guid;
                if (!Guid.TryParse(v, out guid)) {
                    throw new ApiArgumentException("guid");
                }

                provider.Guid = guid;
            });

            if (model.areas != null) {
                if (!(model.areas is JArray)) {
                    throw new ApiArgumentException("areas", ApiArgumentException.EXPECTED_ARRAY);
                }

                IEnumerable<dynamic> areas = model.areas;
                provider.Areas.Clear();
                long uniqueFlag = 1;

                foreach (var area in areas) {
                    string a = DynamicHelper.Value(area);

                    if (a == null) {
                        throw new ApiArgumentException("areas");
                    }

                    var elem = provider.Areas.CreateElement();
                    elem.Name = a;
                    elem.Value = uniqueFlag;
                    uniqueFlag *= 2;
                    provider.Areas.Add(elem);
                }
            }
        }
    }
}
