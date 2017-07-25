// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Microsoft.IIS.Administration.Core;
    using Microsoft.IIS.Administration.Core.Utils;
    using Microsoft.Web.Administration;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO;
    using System.Linq;

    static class ProvidersHelper
    {
        public static readonly Fields SectionRefFields = new Fields("id", "scope");
        public static readonly Fields ProviderRefFields = new Fields("name", "id");

        public static string GetSectionLocation(string id)
        {
            return $"/{Defines.PROVIDERS_SECTION_PATH}/{id}";
        }

        public static string GetProviderLocation(string id)
        {
            return $"/{Defines.PROVIDERS_PATH}/{id}";
        }

        public static object SectionToJsonModelRef(Site site, string path, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return SectionToJsonModel(site, path, SectionRefFields, false);
            }
            else {
                return SectionToJsonModel(site, path, fields, false);
            }
        }

        public static object SectionToJsonModel(Site site, string path, Fields fields = null, bool full = true)
        {
            if (fields == null) {
                fields = Fields.All;
            }

            RewriteId id = new RewriteId(site?.Id, path);
            var section = GetSection(site, path);

            dynamic obj = new ExpandoObject();

            //
            // id
            if (fields.Exists("id")) {
                obj.id = id.Uuid;
            }

            //
            // scope
            if (fields.Exists("scope")) {
                obj.scope = site == null ? string.Empty : site.Name + path;
            }

            //
            // metadata
            if (fields.Exists("metadata")) {
                obj.metadata = ConfigurationUtility.MetadataToJson(section.IsLocallyStored, section.IsLocked, section.OverrideMode, section.OverrideModeEffective);
            }

            //
            // url_rewrite
            if (fields.Exists("url_rewrite")) {
                obj.url_rewrite = RewriteHelper.ToJsonModelRef(site, path);
            }

            return Core.Environment.Hal.Apply(Defines.ProvidersSectionResource.Guid, obj, full);
        }

        public static ProvidersSection GetSection(Site site, string path, string configPath = null)
        {
            return (ProvidersSection)ManagementUnit.GetConfigSection(site?.Id,
                                                                           path,
                                                                           Globals.ProvidersSectionName,
                                                                           typeof(ProvidersSection),
                                                                           configPath);
        }

        public static bool IsSectionLocal(Site site, string path)
        {
            return ManagementUnit.IsSectionLocal(site?.Id,
                                                 path,
                                                 Globals.ProvidersSectionName);
        }

        public static object ProviderToJsonModelRef(Provider provider, Site site, string path, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return ProviderToJsonModel(provider, site, path, ProviderRefFields, false);
            }
            else {
                return ProviderToJsonModel(provider, site, path, fields, false);
            }
        }

        public static object ProviderToJsonModel(Provider provider, Site site, string path, Fields fields = null, bool full = true)
        {
            if (provider == null) {
                return null;
            }

            if (fields == null) {
                fields = Fields.All;
            }

            var providerId = new ProviderId(site?.Id, path, provider.Name);

            dynamic obj = new ExpandoObject();

            //
            // name
            if (fields.Exists("name")) {
                obj.name = provider.Name;
            }

            //
            // id
            if (fields.Exists("id")) {
                obj.id = providerId.Uuid;
            }

            //
            // type
            if (fields.Exists("type")) {
                obj.type = provider.TypeName;
            }

            //
            // settings
            if (fields.Exists("settings")) {
                obj.settings = provider.Settings.Select(s => {
                    return new {
                        name = s.Key,
                        value = s.Value
                    };
                });
            }

            //
            // url_rewrite
            if (fields.Exists("url_rewrite")) {
                obj.url_rewrite = RewriteHelper.ToJsonModelRef(site, path, fields.Filter("url_rewrite"));
            }

            return Core.Environment.Hal.Apply(Defines.ProvidersResource.Guid, obj);
        }

        public static void UpdateSection(dynamic model, Site site, string path, string configPath = null)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            ProvidersSection section = GetSection(site, path, configPath);

            try {
                if (model.metadata != null) {
                    DynamicHelper.If<OverrideMode>((object)model.metadata.override_mode, v => {
                        section.OverrideMode = v;
                    });
                }
            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
        }

        public static void UpdateProvider(dynamic model, Provider provider, ProvidersSection section)
        {
            SetProvider(model, provider, section);
        }

        public static Provider CreateProvider(dynamic model, ProvidersSection section)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            if (string.IsNullOrEmpty(DynamicHelper.Value(model.name))) {
                throw new ApiArgumentException("name");
            }

            if (string.IsNullOrEmpty(DynamicHelper.Value(model.type))) {
                throw new ApiArgumentException("type");
            }

            var provider = section.Providers.CreateElement();

            SetProvider(model, provider, section);

            return provider;
        }

        public static void AddProvider(Provider provider, ProvidersSection section)
        {
            if (provider == null) {
                throw new ArgumentNullException(nameof(provider));
            }

            if (provider.Name == null) {
                throw new ArgumentNullException("provider.Name");
            }

            ProvidersCollection collection = section.Providers;

            if (collection.Any(r => r.Name.Equals(provider.Name))) {
                throw new AlreadyExistsException("provider");
            }

            try {
                collection.Add(provider);
            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
        }

        public static void DeleteProvider(Provider provider, ProvidersSection section)
        {
            if (provider == null) {
                return;
            }

            ProvidersCollection collection = section.Providers;

            // To utilize the remove functionality we must pull the element directly from the collection
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



        private static void SetProvider(dynamic model, Provider provider, ProvidersSection section)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            try {
                //
                // Name, check for already existing name
                string name = DynamicHelper.Value(model.name);
                if (!string.IsNullOrEmpty(name)) {
                    if (!name.Equals(provider.Name, StringComparison.OrdinalIgnoreCase) &&
                            section.Providers.Any(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) {
                        throw new AlreadyExistsException("name");
                    }

                    provider.Name = name;
                }

                DynamicHelper.If((object)model.type, v => provider.TypeName = v);

                //
                // settings
                if (model.settings != null) {

                    IEnumerable<dynamic> settings = model.settings as IEnumerable<dynamic>;

                    if (settings == null) {
                        throw new ApiArgumentException("settings", ApiArgumentException.EXPECTED_ARRAY);
                    }

                    provider.Settings.Clear();

                    foreach (dynamic setting in settings) {
                        if (!(setting is JObject)) {
                            throw new ApiArgumentException("settings.item");
                        }

                        string settingName = DynamicHelper.Value(setting.name);
                        string value = DynamicHelper.Value(setting.value);

                        if (string.IsNullOrEmpty(settingName)) {
                            throw new ApiArgumentException("settings.item.name", "Required");
                        }

                        if (string.IsNullOrEmpty(value)) {
                            throw new ApiArgumentException("settings.item.value", "Required");
                        }

                        var set = provider.Settings.CreateElement();
                        set.Key = settingName;
                        set.Value = value;

                        provider.Settings.Add(set);
                    }
                }
            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
        }
    }
}
