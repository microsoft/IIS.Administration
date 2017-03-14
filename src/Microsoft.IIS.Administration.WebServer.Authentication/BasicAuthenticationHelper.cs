// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Authentication
{
    using Web.Administration;
    using Sites;
    using Core;
    using Core.Utils;
    using System.IO;
    using System.Threading.Tasks;

    class BasicAuthenticationHelper
    {
        public const string FEATURE_NAME = "IIS-BasicAuthentication";
        public const string MODULE = "BasicAuthenticationModule";

        public static void UpdateSettings(dynamic model, Site site, string path, string configPath = null)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            var section = GetSection(site, path, configPath);

            try {

                DynamicHelper.If<bool>((object)model.enabled, v => section.Enabled = v);
                DynamicHelper.If((object)model.default_logon_domain, v => section.DefaultLogonDomain = v);
                DynamicHelper.If((object)model.realm, v => section.Realm = v);

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

        public static object ToJsonModel(Site site, string path)
        {
            var section = GetSection(site, path);

            // Construct id passing possible site and application associated
            BasicAuthId id = new BasicAuthId(site?.Id, path, section.IsLocallyStored);

            var obj = new {
                id = id.Uuid,
                enabled = section.Enabled,
                scope = site == null ? string.Empty : site.Name + path,
                metadata = ConfigurationUtility.MetadataToJson(section.IsLocallyStored, section.IsLocked, section.OverrideMode, section.OverrideModeEffective),
                default_logon_domain = section.DefaultLogonDomain,
                realm = section.Realm,
                website = site == null ? null : SiteHelper.ToJsonModelRef(site),
            };

            return Environment.Hal.Apply(Defines.BasicAuthResource.Guid, obj);
        }

        public static BasicAuthenticationSection GetSection(Site site, string path, string configPath = null)
        {
            return (BasicAuthenticationSection)ManagementUnit.GetConfigSection(site?.Id,
                                                                           path,
                                                                           BasicAuthenticationSection.SECTION_PATH,
                                                                           typeof(BasicAuthenticationSection),
                                                                           configPath);
        }

        public static bool IsSectionLocal(Site site, string path)
        {
            return ManagementUnit.IsSectionLocal(site?.Id,
                                                 path,
                                                 BasicAuthenticationSection.SECTION_PATH);
        }

        public static string GetLocation(string id)
        {
            return $"{Defines.BASIC_AUTH_PATH}/{id}";
        }

        public static bool IsFeatureEnabled()
        {
            return FeaturesUtility.GlobalModuleExists(MODULE);
        }

        public static async Task SetFeatureEnabled(bool enabled)
        {
            IWebServerFeatureManager featureManager = WebServerFeatureManagerAccessor.Instance;
            if (featureManager != null) {
                await (enabled ? featureManager.Enable(FEATURE_NAME) : featureManager.Disable(FEATURE_NAME));
            }
        }
    }
}
