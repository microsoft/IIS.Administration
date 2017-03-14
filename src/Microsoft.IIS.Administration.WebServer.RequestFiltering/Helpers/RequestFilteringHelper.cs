// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering
{
    using Web.Administration;
    using System;
    using System.Collections.Generic;
    using Core.Utils;
    using Core;
    using Sites;
    using System.Linq;
    using System.IO;
    using System.Threading.Tasks;

    static class RequestFilteringHelper
    {
        public const string DISPLAY_NAME = "IIS Request Filtering";
        public const string FEATURE = "IIS-RequestFiltering";
        public const string MODULE = "RequestFilteringModule";

        public static void UpdateFeatureSettings(dynamic model, RequestFilteringSection section)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (section == null) {
                throw new ArgumentNullException("section");
            }

            try {
                DynamicHelper.If<bool>((object)model.allow_unlisted_file_extensions, v => section.FileExtensions.AllowUnlisted = v);
                DynamicHelper.If<bool>((object)model.allow_unlisted_verbs, v => section.Verbs.AllowUnlisted = v);
                DynamicHelper.If<bool>((object)model.allow_high_bit_characters, v => section.AllowHighBitCharacters = v);
                DynamicHelper.If<bool>((object)model.allow_double_escaping, v => section.AllowDoubleEscaping = v);
                DynamicHelper.If((object)model.max_content_length, 0, uint.MaxValue, v => section.RequestLimits.MaxAllowedContentLength = v);
                DynamicHelper.If((object)model.max_url_length, 0, uint.MaxValue, v => section.RequestLimits.MaxUrl = v);
                DynamicHelper.If((object)model.max_query_string_length, 0, uint.MaxValue, v => section.RequestLimits.MaxQueryString = v);

                // Verbs
                if (model.verbs != null) {
                    IEnumerable<dynamic> verbs = (IEnumerable<dynamic>)model.verbs;


                    List<VerbElement> verbList = new List<VerbElement>();

                    // Validate all verbs provided
                    foreach (dynamic verb in verbs) {
                        if (verb.name == null) {
                            throw new ApiArgumentException("verb.name");
                        }
                        if (verb.allowed == null) {
                            throw new ApiArgumentException("verb.allowed");
                        }

                        string name = DynamicHelper.Value(verb.name);
                        bool allowed = DynamicHelper.To<bool>(verb.allowed);

                        VerbElement newVerb = section.Verbs.CreateElement();

                        newVerb.Allowed = allowed;
                        newVerb.Verb = name;

                        // Add to temp list
                        verbList.Add(newVerb);
                    }

                    // Clear configuration's collection
                    section.Verbs.Clear();

                    // Move from temp list to the configuration's collection
                    verbList.ForEach(v => section.Verbs.Add(v));
                }

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

        internal static object ToJsonModel(Site site, string path)
        {
            var section = GetRequestFilteringSection(site, path);

            RequestFilteringId reqId = new RequestFilteringId(site?.Id, path, section.IsLocallyStored);

            var verbs = section.Verbs.Select(v => new {
                name = v.Verb,
                allowed = v.Allowed
            });

            var obj = new {
                id = reqId.Uuid,
                scope = site == null ? string.Empty : site.Name + path,
                metadata = ConfigurationUtility.MetadataToJson(section.IsLocallyStored, section.IsLocked, section.OverrideMode, section.OverrideModeEffective),
                allow_unlisted_file_extensions = section.FileExtensions.AllowUnlisted,
                allow_unlisted_verbs = section.Verbs.AllowUnlisted,
                allow_high_bit_characters = section.AllowHighBitCharacters,
                allow_double_escaping = section.AllowDoubleEscaping,
                max_content_length = section.RequestLimits.MaxAllowedContentLength,
                max_url_length = section.RequestLimits.MaxUrl,
                max_query_string_length = section.RequestLimits.MaxQueryString,
                verbs = verbs,
                website = SiteHelper.ToJsonModelRef(site)
            };

            return Core.Environment.Hal.Apply(Defines.Resource.Guid, obj);
        }

        public static object ToJsonModelRef(Site site, string path)
        {
            var section = GetRequestFilteringSection(site, path);

            RequestFilteringId reqId = new RequestFilteringId(site?.Id, path, section.IsLocallyStored);

            var obj = new {
                id = reqId.Uuid,
                scope = site == null ? string.Empty : site.Name + path
            };

            return Core.Environment.Hal.Apply(Defines.Resource.Guid, obj, false);
        }

        public static RequestFilteringSection GetRequestFilteringSection(Site site, string path, string configPath = null)
        {
            return (RequestFilteringSection)ManagementUnit.GetConfigSection(site?.Id,
                                                                           path,
                                                                           RequestFilteringGlobals.RequestFilteringSectionName,
                                                                           typeof(RequestFilteringSection),
                                                                           configPath);
        }

        public static bool IsSectionLocal(Site site, string path)
        {
            return ManagementUnit.IsSectionLocal(site?.Id,
                                                 path,
                                                 RequestFilteringGlobals.RequestFilteringSectionName);
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
