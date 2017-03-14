// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.StaticContent
{
    using Core;
    using Core.Utils;
    using Sites;
    using System;
    using System.Dynamic;
    using System.Globalization;
    using System.Threading.Tasks;
    using Web.Administration;
    using static MimeTypesGlobals;

    static class StaticContentHelper
    {
        public const string FEATURE = "IIS-StaticContent";
        public const string MODULE = "StaticFileModule";
        public const string DISPLAY_NAME = "Static Content";

        private const string SetETagAttribute = "setEtag";

        public static void UpdateFeatureSettings(dynamic model, StaticContentSection section)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (section == null) {
                throw new ArgumentException("section");
            }

            if (model.client_cache != null) {
                dynamic cache = model.client_cache;

                DynamicHelper.If((object)cache.max_age, 0, (long)TimeSpan.MaxValue.TotalMinutes, v => section.ClientCache.CacheControlMaxAge = TimeSpan.FromMinutes(v));
                DynamicHelper.If((object)cache.control_mode, v => section.ClientCache.CacheControlMode = JsonToCacheControlMode(v));
                DynamicHelper.If((object)cache.control_custom, v => section.ClientCache.CacheControlCustom = v);
                DynamicHelper.If<bool>((object)cache.set_e_tag, v => section.ClientCache.SetETag = v);
                DynamicHelper.If((object)cache.http_expires, v => {
                    DateTime httpExpires;
                    if (!DateTime.TryParseExact(v, "r", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out httpExpires)) {
                        throw new ApiArgumentException("http_expires");
                    }

                    section.ClientCache.HttpExpires = httpExpires;
                });
            }

            DynamicHelper.If((object)model.default_doc_footer, v => section.DefaultDocFooter = v);
            DynamicHelper.If<bool>((object)model.is_doc_footer_file_name, v => {
                if (section.IsDocFooterFileName != v) {
                    section.IsDocFooterFileName = v;
                }
            });
            DynamicHelper.If<bool>((object)model.enable_doc_footer, v => section.EnableDocFooter = v);

            if (model.metadata != null) {

                DynamicHelper.If<OverrideMode>((object)model.metadata.override_mode, v => section.OverrideMode = v);
            }
        }

        internal static object ToJsonModel(Site site, string path)
        {
            var section = GetSection(site, path);

            StaticContentId id = new StaticContentId(site?.Id, path, section.IsLocallyStored);

            var obj = new {
                id = id.Uuid,
                scope = site == null ? string.Empty : site.Name + path,
                metadata = ConfigurationUtility.MetadataToJson(section.IsLocallyStored, section.IsLocked, section.OverrideMode, section.OverrideModeEffective),
                client_cache = CacheToJsonModel(section.ClientCache),
                default_doc_footer = section.DefaultDocFooter,
                is_doc_footer_file_name = section.IsDocFooterFileName,
                enable_doc_footer = section.EnableDocFooter,
                website = SiteHelper.ToJsonModelRef(site)
            };

            return Core.Environment.Hal.Apply(Defines.Resource.Guid, obj);
        }

        public static object ToJsonModelRef(Site site, string path)
        {
            var section = GetSection(site, path);

            StaticContentId id = new StaticContentId(site?.Id, path, section.IsLocallyStored);

            var obj = new {
                id = id.Uuid,
                scope = site == null ? string.Empty : site.Name + path
            };

            return Core.Environment.Hal.Apply(Defines.Resource.Guid, obj, false);
        }

        public static StaticContentSection GetSection(Site site, string path, string configPath = null)
        {
            return (StaticContentSection)ManagementUnit.GetConfigSection(site?.Id,
                                                                           path,
                                                                           StaticContentSectionName,
                                                                           typeof(StaticContentSection),
                                                                           configPath);
        }

        public static bool IsSectionLocal(Site site, string path)
        {
            return ManagementUnit.IsSectionLocal(site?.Id,
                                                 path,
                                                 StaticContentSectionName);
        }

        public static string GetLocation(string id)
        {
            return $"/{Defines.PATH}/{id}";
        }

        private static HttpCacheControlMode JsonToCacheControlMode(string val)
        {
            switch (val) {
                case "disable_cache":
                    return HttpCacheControlMode.DisableCache;
                case "no_control":
                    return HttpCacheControlMode.NoControl;
                case "use_expires":
                    return HttpCacheControlMode.UseExpires;
                case "use_max_age":
                    return HttpCacheControlMode.UseMaxAge;
                default:
                    throw new ApiArgumentException("client_cache.control_mode");
            }
        }

        private static object CacheToJsonModel(StaticContentSection.HttpClientCacheElement cache)
        {

            string controlMode = null;
            switch (cache.CacheControlMode) {
                case MimeTypesGlobals.HttpCacheControlMode.DisableCache:
                    controlMode = "disable_cache";
                    break;
                case MimeTypesGlobals.HttpCacheControlMode.NoControl:
                    controlMode = "no_control";
                    break;
                case MimeTypesGlobals.HttpCacheControlMode.UseExpires:
                    controlMode = "use_expires";
                    break;
                case MimeTypesGlobals.HttpCacheControlMode.UseMaxAge:
                    controlMode = "use_max_age";
                    break;
                default:
                    break;
            }

            dynamic obj = new ExpandoObject();

            obj.control_mode = controlMode;
            obj.max_age = (long)cache.CacheControlMaxAge.TotalMinutes;
            obj.http_expires = cache.HttpExpires.ToString("r");
            obj.control_custom = cache.CacheControlCustom;

            if (cache.Schema.HasAttribute(SetETagAttribute)) {
                obj.set_e_tag = cache.SetETag;
            }

            return obj;
        }

        public static bool IsFeatureEnabled()
        {
            return FeaturesUtility.GlobalModuleExists(MODULE);
        }

        public static async Task SetFeatureEnabled(bool enabled)
        {
            IWebServerFeatureManager featureManager = WebServerFeatureManagerAccessor.Instance;
            if (featureManager != null) {
                await (enabled ? featureManager.Enable(FEATURE) : featureManager.Disable(FEATURE));
            }
        }
    }
}
