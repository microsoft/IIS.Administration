// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using System.Linq;
    using Web.Administration;

    static class ServerVariablesHelper
    {
        public static string GetLocation(string id)
        {
            return $"/{Defines.SERVER_VARIABLES_PATH}/{id}";
        }

        public static object ToJsonModel(Site site, string path)
        {
            ServerVariablesId serverVariablesId = new ServerVariablesId(site?.Id, path);
            var section = GetSection(site, path);

            var obj = new
            {
                id = serverVariablesId.Uuid,
                scope = site == null ? string.Empty : site.Name + path,
                server_variables = section.AllowedServerVariables.Select(v => v.Name),
                metadata = ConfigurationUtility.MetadataToJson(section.IsLocallyStored, section.IsLocked, section.OverrideMode, section.OverrideModeEffective),
                url_rewrite = RewriteHelper.ToJsonModelRef(site, path)
            };

            return Core.Environment.Hal.Apply(Defines.ServerVariablesResource.Guid, obj);
        }

        public static AllowedServerVariablesSection GetSection(Site site, string path, string configPath = null)
        {
            return (AllowedServerVariablesSection)ManagementUnit.GetConfigSection(site?.Id,
                                                                           path,
                                                                           Globals.AllowedServerVariablesSectionName,
                                                                           typeof(AllowedServerVariablesSection),
                                                                           configPath);
        }

        public static bool IsSectionLocal(Site site, string path)
        {
            return ManagementUnit.IsSectionLocal(site?.Id,
                                                 path,
                                                 Globals.AllowedServerVariablesSectionName);
        }
    }
}

