// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Handlers
{
    using Core.Utils;
    using Core;
    using Web.Administration;
    using Newtonsoft.Json;
    using Sites;
    using System;
    using System.Collections.Generic;
    using System.IO;

    public static class HandlersHelper
    {
        internal static object ToJsonModel(Site site, string path)
        {
            HandlersSection section = GetHandlersSection(site, path);

            HandlersId id = new HandlersId(site?.Id, path, section.IsLocallyStored);
            
            // Access Policy
            HandlerAccessPolicy accessPolicy = section.AccessPolicy;

            Dictionary<string, bool> allowedAccess = new Dictionary<string, bool>();
            allowedAccess.Add("read", accessPolicy.HasFlag(HandlerAccessPolicy.Read));
            allowedAccess.Add("write", accessPolicy.HasFlag(HandlerAccessPolicy.Write));
            allowedAccess.Add("execute", accessPolicy.HasFlag(HandlerAccessPolicy.Execute));
            allowedAccess.Add("source", accessPolicy.HasFlag(HandlerAccessPolicy.Source));
            allowedAccess.Add("script", accessPolicy.HasFlag(HandlerAccessPolicy.Script));

            Dictionary<string, bool> remoteAccessPrevention = new Dictionary<string, bool>();
            remoteAccessPrevention.Add("write", accessPolicy.HasFlag(HandlerAccessPolicy.NoRemoteWrite));
            remoteAccessPrevention.Add("read", accessPolicy.HasFlag(HandlerAccessPolicy.NoRemoteRead));
            remoteAccessPrevention.Add("execute", accessPolicy.HasFlag(HandlerAccessPolicy.NoRemoteExecute));
            remoteAccessPrevention.Add("script", accessPolicy.HasFlag(HandlerAccessPolicy.NoRemoteScript));

            var obj = new {
                id = id.Uuid,
                scope = site == null ? string.Empty : site.Name + path,
                metadata = ConfigurationUtility.MetadataToJson(section.IsLocallyStored, section.IsLocked, section.OverrideMode, section.OverrideModeEffective),
                allowed_access = allowedAccess,
                remote_access_prevention = remoteAccessPrevention,
                website = SiteHelper.ToJsonModelRef(site)
            };

            return Core.Environment.Hal.Apply(Defines.Resource.Guid, obj);
        }

        public static void UpdateFeatureSettings(dynamic model, HandlersSection section)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (section == null) {
                throw new ArgumentNullException("section");
            }
            
            try {
                if (model.allowed_access != null) {

                    Dictionary<string, bool> accessPolicyDictionary = null;
                    
                    try {
                        accessPolicyDictionary = JsonConvert.DeserializeObject<Dictionary<string, bool>>(model.allowed_access.ToString());
                    }
                    catch (JsonSerializationException e) {
                        throw new ApiArgumentException("allowed_access", e);
                    }

                    if (accessPolicyDictionary == null) {
                        throw new ApiArgumentException("allowed_access");
                    }

                    Dictionary<string, HandlerAccessPolicy> accessPolicyMap = new Dictionary<string, HandlerAccessPolicy>() {
                        { "read", HandlerAccessPolicy.Read },
                        { "write", HandlerAccessPolicy.Write },
                        { "execute", HandlerAccessPolicy.Execute },
                        { "source", HandlerAccessPolicy.Source },
                        { "script", HandlerAccessPolicy.Script }
                    };
                    
                    foreach (var key in accessPolicyMap.Keys) {
                        if (accessPolicyDictionary.ContainsKey(key)) {
                            if (accessPolicyDictionary[key]) {
                                section.AccessPolicy |= accessPolicyMap[key];
                            }
                            else {
                                section.AccessPolicy &= ~accessPolicyMap[key];
                            }
                        }
                    }
                }

                if (model.remote_access_prevention != null) {

                    Dictionary<string, bool> remoteAccessDictionary = null;

                    try {
                        remoteAccessDictionary = JsonConvert.DeserializeObject<Dictionary<string, bool>>(model.remote_access_prevention.ToString());
                    }
                    catch (JsonSerializationException e) {
                        throw new ApiArgumentException("remote_access_prevention", e);
                    }

                    if (remoteAccessDictionary == null) {
                        throw new ApiArgumentException("remote_access_prevention");
                    }

                    Dictionary<string, HandlerAccessPolicy> remoteAccessMap = new Dictionary<string, HandlerAccessPolicy>() {
                        { "read", HandlerAccessPolicy.NoRemoteRead },
                        { "write", HandlerAccessPolicy.NoRemoteWrite },
                        { "execute", HandlerAccessPolicy.NoRemoteExecute },
                        { "script", HandlerAccessPolicy.NoRemoteScript }
                    };
                    
                    foreach (var key in remoteAccessMap.Keys) {
                        if (remoteAccessDictionary.ContainsKey(key)) {
                            if (remoteAccessDictionary[key]) {
                                section.AccessPolicy |= remoteAccessMap[key];
                            }
                            else {
                                section.AccessPolicy &= ~remoteAccessMap[key];
                            }
                        }
                    }
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

        public static object ToJsonModelRef(Site site, string path)
        {
            HandlersSection section = GetHandlersSection(site, path);

            HandlersId id = new HandlersId(site?.Id, path, section.IsLocallyStored);


            var obj = new {
                id = id.Uuid,
                scope = site == null ? string.Empty : site.Name + path
            };

            return Core.Environment.Hal.Apply(Defines.Resource.Guid, obj, false);
        }



        public static HandlersSection GetHandlersSection(Site site, string path, string configPath = null)
        {
            return (HandlersSection)ManagementUnit.GetConfigSection(site?.Id,
                                                                           path,
                                                                           HandlersGlobals.HandlersSectionName,
                                                                           typeof(HandlersSection),
                                                                           configPath);
        }

        public static bool IsSectionLocal(Site site, string path)
        {
            return ManagementUnit.IsSectionLocal(site?.Id,
                                                 path,
                                                 HandlersGlobals.HandlersSectionName);
        }

        public static string GetLocation(string id)
        {
            return $"/{Defines.PATH}/{id}";
        }
    }
}
