// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.DefaultDocuments
{
    using Web.Administration;
    using Core;
    using Sites;
    using Core.Utils;
    using System.IO;
    using System.Threading.Tasks;

    static class DefaultDocumentHelper
    {
        public const string FEATURE_NAME = "IIS-DefaultDocument";
        public const string MODULE = "DefaultDocumentModule";

        internal static object ToJsonModel(Site site, string path)
        {
            var section = GetDefaultDocumentSection(site, path);

            // Construct id passing possible site and application associated
            DefaultDocumentId docId = new DefaultDocumentId(site?.Id, path, section.IsLocallyStored);

            var obj = new {
                id = docId.Uuid,
                enabled = section.Enabled,
                scope = site == null ? string.Empty : site.Name + path,
                metadata = ConfigurationUtility.MetadataToJson(section.IsLocallyStored, section.IsLocked, section.OverrideMode, section.OverrideModeEffective),
                website = SiteHelper.ToJsonModelRef(site)
            };

            return Environment.Hal.Apply(Defines.Resource.Guid, obj);
        }

        public static object ToJsonModelRef(Site site, string path)
        {
            var section = GetDefaultDocumentSection(site, path);

            // Construct id passing possible site and application associated
            DefaultDocumentId docId = new DefaultDocumentId(site?.Id, path, section.IsLocallyStored);

            var obj = new {
                id = docId.Uuid,
                scope = site == null ? string.Empty : site.Name + path
            };

            return Environment.Hal.Apply(Defines.Resource.Guid, obj, false);
        }

        public static void UpdateFeatureSettings(dynamic model, DefaultDocumentSection section)
        {
            try {
                DynamicHelper.If<bool>((object)model.enabled, v => section.Enabled = v);

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

        public static DefaultDocumentSection GetDefaultDocumentSection(Site site, string path, string configPath = null)
        {
            return (DefaultDocumentSection)ManagementUnit.GetConfigSection(site?.Id,
                                                                           path,
                                                                           DefaultDocumentGlobals.DefaultDocumentSectionName,
                                                                           typeof(DefaultDocumentSection),
                                                                           configPath);
        }

        public static bool IsSectionLocal(Site site, string path) {
            return ManagementUnit.IsSectionLocal(site?.Id, 
                                                 path, 
                                                 DefaultDocumentGlobals.DefaultDocumentSectionName);
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
                await (enabled ? featureManager.Enable(FEATURE_NAME) : featureManager.Disable(FEATURE_NAME));
            }
        }
    }
}
