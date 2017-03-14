// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.DirectoryBrowsing
{
    using Core;
    using Core.Utils;
    using Web.Administration;
    using Newtonsoft.Json;
    using Sites;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    static class DirectoryBrowsingHelper
    {
        public const string FEATURE = "IIS-DirectoryBrowsing";
        public const string MODULE = "DirectoryListingModule";

        public static void UpdateSettings(dynamic model, Site site, string path, string configPath = null)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            var section = GetDirectoryBrowseSection(site, path, configPath);

            try {

                DynamicHelper.If<bool>((object)model.enabled, v => section.Enabled = v);

                if (model.allowed_attributes != null) {

                    Dictionary<string, bool> showDictionary = JsonConvert.DeserializeObject<Dictionary<string, bool>>(model.allowed_attributes.ToString());

                    if (showDictionary == null) {
                        throw new ApiArgumentException("allowed_attributes");
                    }

                    DirectoryBrowseShowFlags showFlags = section.ShowFlags;
                    if (showDictionary.ContainsKey("date")) {
                        if (showDictionary["date"]) {
                            showFlags |= DirectoryBrowseShowFlags.Date;
                        }
                        else {
                            showFlags &= ~DirectoryBrowseShowFlags.Date;
                        }
                    }
                    if (showDictionary.ContainsKey("time")) {
                        if (showDictionary["time"]) {
                            showFlags |= DirectoryBrowseShowFlags.Time;
                        }
                        else {
                            showFlags &= ~DirectoryBrowseShowFlags.Time;
                        }
                    }
                    if (showDictionary.ContainsKey("size")) {
                        if (showDictionary["size"]) {
                            showFlags |= DirectoryBrowseShowFlags.Size;
                        }
                        else {
                            showFlags &= ~DirectoryBrowseShowFlags.Size;
                        }
                    }
                    if (showDictionary.ContainsKey("extension")) {
                        if (showDictionary["extension"]) {
                            showFlags |= DirectoryBrowseShowFlags.Extension;
                        }
                        else {
                            showFlags &= ~DirectoryBrowseShowFlags.Extension;
                        }
                    }
                    if (showDictionary.ContainsKey("long_date")) {
                        if (showDictionary["long_date"]) {
                            showFlags |= DirectoryBrowseShowFlags.LongDate;
                        }
                        else {
                            showFlags &= ~DirectoryBrowseShowFlags.LongDate;
                        }
                    }

                    section.ShowFlags = showFlags;
                }


                if (model.metadata != null) {

                    DynamicHelper.If<OverrideMode>((object)model.metadata.override_mode, v => section.OverrideMode = v);
                }
            }
            catch (JsonSerializationException e) {
                throw new ApiArgumentException("allowed_attributes", e);
            }
            catch(FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }

        }

        internal static object ToJsonModel(Site site, string path)
        {
            var section = GetDirectoryBrowseSection(site, path);

            DirectoryBrowsingId dirbId = new DirectoryBrowsingId(site?.Id, path, section.IsLocallyStored);

            DirectoryBrowseShowFlags showFlags = section.ShowFlags;

            Dictionary<string, bool> showDictionary = new Dictionary<string, bool>();
            showDictionary.Add("date", showFlags.HasFlag(DirectoryBrowseShowFlags.Date));
            showDictionary.Add("time", showFlags.HasFlag(DirectoryBrowseShowFlags.Time));
            showDictionary.Add("size", showFlags.HasFlag(DirectoryBrowseShowFlags.Size));
            showDictionary.Add("extension", showFlags.HasFlag(DirectoryBrowseShowFlags.Extension));
            showDictionary.Add("long_date", showFlags.HasFlag(DirectoryBrowseShowFlags.LongDate));

            var obj = new {
                id = dirbId.Uuid,
                scope = site == null ? string.Empty : site.Name + path,
                metadata = ConfigurationUtility.MetadataToJson(section.IsLocallyStored, section.IsLocked, section.OverrideMode, section.OverrideModeEffective),
                enabled = section.Enabled,
                allowed_attributes = showDictionary,
                website = SiteHelper.ToJsonModelRef(site),
            };

            return Environment.Hal.Apply(Defines.Resource.Guid, obj);
        }

        public static DirectoryBrowseSection GetDirectoryBrowseSection(Site site, string path, string configPath = null)
        {
            return (DirectoryBrowseSection)ManagementUnit.GetConfigSection(site?.Id,
                                                                           path,
                                                                           DirectoryBrowseGlobals.DirectoryBrowseSectionName,
                                                                           typeof(DirectoryBrowseSection),
                                                                           configPath);
        }

        public static bool IsSectionLocal(Site site, string path)
        {
            return ManagementUnit.IsSectionLocal(site?.Id,
                                                 path,
                                                 DirectoryBrowseGlobals.DirectoryBrowseSectionName);
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
