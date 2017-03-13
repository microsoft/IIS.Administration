// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Delegation
{
    using Core.Utils;
    using Core;
    using Sites;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Web.Administration;

    public static class DelegationHelper
    {
        public const string XPATH = "system.webServer";

        public static void Update(ConfigurationSection section, dynamic model)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if(section == null) {
                throw new ArgumentException("section");
            }
            
            OverrideMode? overrideMode = DynamicHelper.To<OverrideMode>(model.override_mode);
            
            if (overrideMode != null && overrideMode.Value != section.OverrideMode) {
                section.OverrideMode = overrideMode.Value;
            }
        }

        internal static object SectionToJsonModel(ConfigurationSection section, Site site, string path, string configScope)
        {
            if(section == null) {
                return null;
            }

            // 
            configScope = configScope != null ? configScope : (site == null ? "" : $"{site.Name}{path}");

            SectionId id = new SectionId(site?.Id, path, section.SectionPath.Replace($"{XPATH}/", string.Empty), configScope);

            var obj = new {
                name = section.SectionPath,
                id = id.Uuid,
                scope = site == null ? string.Empty : site.Name + path,
                config_scope = configScope,
                override_mode = Enum.GetName(typeof(OverrideMode), section.OverrideMode).ToLower(),
                override_mode_effective = Enum.GetName(typeof(OverrideMode), section.OverrideModeEffective).ToLower(),
                website = SiteHelper.ToJsonModelRef(site)
            };

            return Core.Environment.Hal.Apply(Defines.Resource.Guid, obj);
        }
        public static object SectionToJsonModelRef(ConfigurationSection section, Site site, string path, string configScope)
        {
            if (section == null) {
                return null;
            }

            configScope = configScope != null ? configScope : (site == null ? "" : $"{site.Name}{path}");

            SectionId id = new SectionId(site?.Id, path, section.SectionPath.Replace($"{XPATH}/", string.Empty), configScope);

            var obj = new {
                name = section.SectionPath,
                id = id.Uuid,
                scope = site == null ? string.Empty : site.Name + path,
                override_mode = Enum.GetName(typeof(OverrideMode), section.OverrideMode).ToLower(),
                override_mode_effective = Enum.GetName(typeof(OverrideMode), section.OverrideModeEffective).ToLower(),
            };

            return Core.Environment.Hal.Apply(Defines.Resource.Guid, obj, false);
        }

        public static List<ConfigurationSection> GetWebServerSections(Site site, string path, string configScope = null)
        {
            if(site != null && path == null) {
                throw new ArgumentNullException("path");
            }

            // Get the location to be used when getting sections from the configuration
            string location = site == null ? null : ManagementUnit.GetLocationTag(site.Id, path, configScope);

            // Get the target configuration file using configScope
            Configuration configuration = ManagementUnit.GetConfiguration(site?.Id, path, configScope); ;


            List<ConfigurationSection> sectionList = new List<ConfigurationSection>();

            SectionGroup webServerGroup = configuration.GetEffectiveSectionGroup().SectionGroups.FirstOrDefault(group => group.Name.Equals(XPATH));

            if(webServerGroup == null) {
                return sectionList;
            }

            FillWithSubSections(sectionList, webServerGroup, webServerGroup.Name, configuration, location);

            return sectionList;
        }

        private static void FillWithSubSections(List<ConfigurationSection> sectionList, SectionGroup group, string groupXPath, Configuration configuration, string location)
        {
            foreach (SectionDefinition section in group.Sections) {
                var sectionPath = $"{groupXPath}/{section.Name}";
                ConfigurationSection sect = null;

                try {
                    sect = location == null ? configuration.GetSection(sectionPath) : configuration.GetSection(sectionPath, location);
                }
                catch (FileLoadException e) {
                    throw new LockedException(sectionPath, e);
                }

                sectionList.Add(sect);
            }

            foreach (SectionGroup subGroup in group.SectionGroups) {
                FillWithSubSections(sectionList, subGroup, $"{groupXPath}/{subGroup.Name}", configuration, location);
            }
        }
    }
}
