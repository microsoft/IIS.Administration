// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Authentication
{
    using Core;
    using Sites;
    using System;
    using System.IO;
    using Web.Administration;
    using Core.Utils;

    static class AnonymousAuthenticationHelper
    {
        public static void UpdateSettings(dynamic model, Site site, string path, string configPath = null)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            var section = GetSection(site, path, configPath);

            try {

                DynamicHelper.If<bool>((object)model.enabled, v => section.Enabled = v);
                DynamicHelper.If((object)model.user, v => section.UserName = v);
                DynamicHelper.If((object)model.password, v => section.Password = v);

                string user = DynamicHelper.Value(model.user);

                // Empty username is for application pool identity
                bool deletePassword = (user != null) && ( user.Equals(string.Empty) || user.Equals("iusr", StringComparison.OrdinalIgnoreCase) );
                if (deletePassword) {
                    ConfigurationAttribute passwordAttr = section.GetAttribute("password");
                    passwordAttr.Delete();
                }

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
            AnonAuthId id = new AnonAuthId(site?.Id, path, section.IsLocallyStored);

            var obj = new {
                id = id.Uuid,
                enabled = section.Enabled,
                scope = site == null ? string.Empty : site.Name + path,
                metadata = ConfigurationUtility.MetadataToJson(section.IsLocallyStored, section.IsLocked, section.OverrideMode, section.OverrideModeEffective),
                user = section.UserName,
                website = site == null ? null : SiteHelper.ToJsonModelRef(site),
            };

            return Core.Environment.Hal.Apply(Defines.AnonAuthResource.Guid, obj);
        }

        public static AnonymousAuthenticationSection GetSection(Site site, string path, string configPath = null)
        {
            return (AnonymousAuthenticationSection)ManagementUnit.GetConfigSection(site?.Id,
                                                                           path,
                                                                           AnonymousAuthenticationSection.SECTION_PATH,
                                                                           typeof(AnonymousAuthenticationSection),
                                                                           configPath);
        }

        public static bool IsSectionLocal(Site site, string path)
        {
            return ManagementUnit.IsSectionLocal(site?.Id,
                                                 path,
                                                 AnonymousAuthenticationSection.SECTION_PATH);
        }
    }
}
