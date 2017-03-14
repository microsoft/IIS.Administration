// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRedirect
{
    using Core;
    using Core.Utils;
    using Sites;
    using System.IO;
    using System.Threading.Tasks;
    using Web.Administration;

    class RedirectHelper
    {
        public const string FEATURE = "IIS-HttpRedirect";
        public const string MODULE = "HttpRedirectionModule";

        public static object ToJsonModel(Site site, string path)
        {
            var section = GetRedirectSection(site, path);
            var redId = new RedirectId(site?.Id, path, section.IsLocallyStored);

            var obj = new {
                id = redId.Uuid,
                enabled = section.Enabled,
                preserve_filename = section.ChildOnly,
                destination = section.Destination,
                absolute = section.ExactDestination,
                status_code = (int) section.HttpResponseStatus,
                scope = site == null ? string.Empty : site.Name + path,
                metadata = ConfigurationUtility.MetadataToJson(section.IsLocallyStored, section.IsLocked, section.OverrideMode, section.OverrideModeEffective),
                website = SiteHelper.ToJsonModelRef(site)
            };

            return Environment.Hal.Apply(Defines.Resource.Guid, obj);
        }

        public static object ToJsonModelRef(Site site, string path)
        {
            var section = GetRedirectSection(site, path);            
            var redId = new RedirectId(site?.Id, path, section.IsLocallyStored);

            var obj = new {
                enabled = section.Enabled,
                id = redId.Uuid,
                scope = site == null ? string.Empty : site.Name + path
            };

            return Environment.Hal.Apply(Defines.Resource.Guid, obj, false);
        }

        public static void UpdateFeatureSettings(dynamic model, HttpRedirectSection section)
        {
            try {
                DynamicHelper.If<bool>((object)model.enabled, v => section.Enabled = v);
                DynamicHelper.If<bool>((object)model.preserve_filename, v => section.ChildOnly = v);
                DynamicHelper.If((object)model.destination, v => section.Destination = v);
                DynamicHelper.If<bool>((object)model.absolute, v => section.ExactDestination = v);
                DynamicHelper.If<int>((object)model.status_code, v => section.HttpResponseStatus = (RedirectHttpResponseStatus) v);

                if (model.metadata != null) {
                    DynamicHelper.If<OverrideMode>((object)model.metadata.override_mode, v => section.OverrideMode = v);
                }

            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
        }

        public static HttpRedirectSection GetRedirectSection(Site site, string path, string configPath = null)
        {
            return (HttpRedirectSection)ManagementUnit.GetConfigSection(site?.Id,
                                                                           path,
                                                                           HttpRedirectSection.HttpRedirectSectionName,
                                                                           typeof(HttpRedirectSection),
                                                                           configPath);
        }

        public static bool IsSectionLocal(Site site, string path)
        {
            return ManagementUnit.IsSectionLocal(site?.Id,
                                                 path,
                                                 HttpRedirectSection.HttpRedirectSectionName);
        }

        public static string GetLocation(string id)
        {
            return $"/{Defines.PATH}/{id}";
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
