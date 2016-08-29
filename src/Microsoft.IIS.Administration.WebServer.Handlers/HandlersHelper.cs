// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Handlers
{
    using Core.Utils;
    using Microsoft.IIS.Administration.Core;
    using Microsoft.Web.Administration;
    using Newtonsoft.Json;
    using Sites;
    using System;
    using System.Collections.Generic;
    using System.IO;

    public static class HandlersHelper
    {
        public static object ToJsonModel(Site site, string path)
        {
            HandlersSection section = GetHandlersSection(site, path);

            HandlersId id = new HandlersId(site?.Id, path, section.IsLocallyStored);
            
            // Access Policy
            HandlerAccessPolicy accessPolicy = section.AccessPolicy;

            Dictionary<string, bool> policyDictionary = new Dictionary<string, bool>();
            policyDictionary.Add("read", accessPolicy.HasFlag(HandlerAccessPolicy.Read));
            policyDictionary.Add("write", accessPolicy.HasFlag(HandlerAccessPolicy.Write));
            policyDictionary.Add("execute", accessPolicy.HasFlag(HandlerAccessPolicy.Execute));
            policyDictionary.Add("source", accessPolicy.HasFlag(HandlerAccessPolicy.Source));
            policyDictionary.Add("script", accessPolicy.HasFlag(HandlerAccessPolicy.Script));
            policyDictionary.Add("no_remote_write", accessPolicy.HasFlag(HandlerAccessPolicy.NoRemoteWrite));
            policyDictionary.Add("no_remote_read", accessPolicy.HasFlag(HandlerAccessPolicy.NoRemoteRead));
            policyDictionary.Add("no_remote_execute", accessPolicy.HasFlag(HandlerAccessPolicy.NoRemoteExecute));
            policyDictionary.Add("no_remote_script", accessPolicy.HasFlag(HandlerAccessPolicy.NoRemoteScript));

            var obj = new {
                id = id.Uuid,
                scope = site == null ? string.Empty : site.Name + path,
                metadata = ConfigurationUtility.MetadataToJson(section.IsLocallyStored, section.IsLocked, section.OverrideMode, section.OverrideModeEffective),
                access_policy = policyDictionary,
                website = SiteHelper.ToJsonModelRef(site)
            };

            return Core.Environment.Hal.Apply(Defines.Resource.Guid, obj);
        }

        public static void UpdateFeatureSettings(dynamic model, HandlersSection section)
        {
            if(model == null) {
                throw new ApiArgumentException("model");
            }
            if (section == null) {
                throw new ArgumentNullException("section");
            }
            
            try {

                if (model.access_policy != null) {

                    Dictionary<string, bool> accessPolicyDictionary = JsonConvert.DeserializeObject<Dictionary<string, bool>>(model.access_policy.ToString());

                    if (accessPolicyDictionary == null) {
                        throw new ApiArgumentException("access_policy");
                    }

                    HandlerAccessPolicy accessPolicy = HandlerAccessPolicy.None;
                    if (accessPolicyDictionary.ContainsKey("read") && accessPolicyDictionary["read"] == true) {
                        accessPolicy |= HandlerAccessPolicy.Read;
                    }
                    if (accessPolicyDictionary.ContainsKey("write") && accessPolicyDictionary["write"] == true) {
                        accessPolicy |= HandlerAccessPolicy.Write;
                    }
                    if (accessPolicyDictionary.ContainsKey("execute") && accessPolicyDictionary["execute"] == true) {
                        accessPolicy |= HandlerAccessPolicy.Execute;
                    }
                    if (accessPolicyDictionary.ContainsKey("source") && accessPolicyDictionary["source"] == true) {
                        accessPolicy |= HandlerAccessPolicy.Source;
                    }
                    if (accessPolicyDictionary.ContainsKey("script") && accessPolicyDictionary["script"] == true) {
                        accessPolicy |= HandlerAccessPolicy.Script;
                    }
                    if (accessPolicyDictionary.ContainsKey("no_remote_write") && accessPolicyDictionary["no_remote_write"] == true) {
                        accessPolicy |= HandlerAccessPolicy.NoRemoteWrite;
                    }
                    if (accessPolicyDictionary.ContainsKey("no_remote_read") && accessPolicyDictionary["no_remote_read"] == true) {
                        accessPolicy |= HandlerAccessPolicy.NoRemoteRead;
                    }
                    if (accessPolicyDictionary.ContainsKey("no_remote_execute") && accessPolicyDictionary["no_remote_execute"] == true) {
                        accessPolicy |= HandlerAccessPolicy.NoRemoteExecute;
                    }
                    if (accessPolicyDictionary.ContainsKey("no_remote_script") && accessPolicyDictionary["no_remote_script"] == true) {
                        accessPolicy |= HandlerAccessPolicy.NoRemoteScript;
                    }

                    section.AccessPolicy = accessPolicy;
                }

                if (model.metadata != null) {
                    DynamicHelper.If<OverrideMode>((object)model.metadata.override_mode, v => section.OverrideMode = v);
                }

            }
            catch (JsonSerializationException e) {
                throw new ApiArgumentException("access_policy", e);
            }
            catch(FileLoadException e) {
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
