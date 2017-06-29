// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Microsoft.IIS.Administration.Core;
    using Microsoft.IIS.Administration.Core.Utils;
    using System.Collections.Generic;
    using System.IO;
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
            RewriteId serverVariablesId = new RewriteId(site?.Id, path);
            var section = GetSection(site, path);

            var obj = new
            {
                id = serverVariablesId.Uuid,
                scope = site == null ? string.Empty : site.Name + path,
                server_variables = section.AllowedServerVariables.Select(v => v.Name),
                metadata = ConfigurationUtility.MetadataToJson(section.IsLocallyStored, section.IsLocked, section.OverrideMode, section.OverrideModeEffective),
                url_rewrite = RewriteHelper.ToJsonModelRef(site, path)
            };

            return Environment.Hal.Apply(Defines.ServerVariablesResource.Guid, obj);
        }

        public static void UpdateFeatureSettings(dynamic model, Site site, string path, string configPath = null)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            AllowedServerVariablesSection section = ServerVariablesHelper.GetSection(site, path, configPath);

            try {

                if (model.server_variables != null) {
                    IEnumerable<dynamic> variables = model.server_variables as IEnumerable<dynamic>;

                    if (variables == null) {
                        throw new ApiArgumentException("server_variables", ForbiddenArgumentException.EXPECTED_ARRAY);
                    }

                    List<string> variableList = new List<string>();

                    // Validate all verbs provided
                    foreach (dynamic variable in variables) {
                        string var = DynamicHelper.Value(variable);

                        if (string.IsNullOrEmpty(var)) {
                            throw new ApiArgumentException("server_variables.item");
                        }

                        variableList.Add(var);
                    }

                    // Clear configuration's collection
                    section.AllowedServerVariables.Clear();

                    // Move from temp list to the configuration's collection
                    variableList.ForEach(v => section.AllowedServerVariables.Add(v));
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

