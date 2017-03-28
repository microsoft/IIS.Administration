// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer
{
    using System;
    using System.Linq;
    using Web.Administration;
    public static class ConfigurationUtility {

        public static bool ShouldPersist(string oldValue, string newValue) {
            if (String.IsNullOrEmpty(oldValue)) {
                return !String.IsNullOrEmpty(newValue);
            }
            else {
                if (newValue == null) {
                    return false;
                }
                else if (newValue.Length == 0) {
                    return true;
                }
                else {
                    return !oldValue.Equals(newValue, StringComparison.Ordinal);
                }
            }
        }

        public static object MetadataToJson(bool isLocal, bool isLocked, OverrideMode overrideMode, OverrideMode overrideModeEffective)
        {
            return new {
                is_local = isLocal,
                is_locked = isLocked,
                override_mode = Enum.GetName(typeof(OverrideMode), overrideMode).ToLower(),
                override_mode_effective = Enum.GetName(typeof(OverrideMode), overrideModeEffective).ToLower()
            };
        }

        public static bool HasAttribute(this ConfigurationElementSchema schema, string name)
        {
            return schema.AttributeSchemas.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public static bool HasChildElement(this ConfigurationElementSchema schema, string name)
        {
            return schema.ChildElementSchemas != null && schema.ChildElementSchemas.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public static bool HasSection(this Configuration configuration, string xPath)
        {
            var xParts = xPath.Split('/');
            var group = configuration.RootSectionGroup;

            for (var i = 0; i < xParts.Length; i++) {

                if (i < xParts.Length - 1) {
                    group = group.SectionGroups.Where(g => g.Name.Equals(xParts[i], StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                    if (group == null) {
                        return false;
                    }
                }
                else {
                    var section = group.Sections.Where(s => s.Name.Equals(xParts[i], StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                    if (section == null) {
                        return false;
                    }
                }

            }

            return true;
        }
    }
}
