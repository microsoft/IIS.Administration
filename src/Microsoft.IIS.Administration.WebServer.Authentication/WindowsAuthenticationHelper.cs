// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Authentication
{
    using Core;
    using Sites;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Web.Administration;
    using Core.Utils;
    using System.Linq;
    using Newtonsoft.Json.Linq;
    using System.Threading.Tasks;

    static class WindowsAuthenticationHelper
    {
        public const string FEATURE_NAME = "IIS-WindowsAuthentication";
        public const string MODULE = "WindowsAuthenticationModule";

        public static void UpdateSettings(dynamic model, Site site, string path, string configPath = null)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            var section = GetSection(site, path, configPath);

            try {                
                DynamicHelper.If<bool>((object)model.enabled, v => section.Enabled = v);
                DynamicHelper.If<bool>((object)model.use_kernel_mode, v => section.UseKernelMode = v);
                DynamicHelper.If<TokenChecking>((object)model.token_checking, v => section.TokenCheckingAttribute = v);

                if (model.providers != null) {
                    if (!(model.providers is JArray)) {
                        throw new ApiArgumentException("providers");
                    }

                    IEnumerable<dynamic> providers = model.providers;
                    var enabledProviders = section.Providers;
                    var availableProviders = ProvidersUtil.GetAvailableProvidersList();

                    foreach (dynamic provider in providers) {
                        if (!(provider is JObject)) {
                            throw new ApiArgumentException("provider");
                        }
                        if (string.IsNullOrEmpty(DynamicHelper.Value(provider.name))) {
                            throw new ApiArgumentException("provider.name");
                        }
                        if (DynamicHelper.To<bool>(provider.enabled) == null) {
                            throw new ApiArgumentException("provider.enabled");
                        }

                        string name = DynamicHelper.Value(provider.name);
                        bool providerEnabled = DynamicHelper.To<bool>(provider.enabled);

                        if (providerEnabled && !enabledProviders.Contains(name)) {
                            // Make sure the provider is available to be added to enabled providers
                            if (!availableProviders.Contains(name)) {
                                throw new NotFoundException($"provider: {name}");
                            }

                            enabledProviders.Add(name);
                        }
                        else if (!providerEnabled && enabledProviders.Contains(name)) {
                            enabledProviders.Remove(name);
                        }
                    }

                    section.Providers = enabledProviders;
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
            WinAuthId id = new WinAuthId(site?.Id, path, section.IsLocallyStored);

            var obj = new {
                id = id.Uuid,
                enabled = section.Enabled,
                scope = site == null ? string.Empty : site.Name + path,
                metadata = ConfigurationUtility.MetadataToJson(section.IsLocallyStored, section.IsLocked, section.OverrideMode, section.OverrideModeEffective),
                use_kernel_mode = section.UseKernelMode,
                token_checking = Enum.GetName(typeof(TokenChecking), section.TokenCheckingAttribute).ToLower(),
                providers = ProvidersJsonModel(section),
                website = site == null ? null : SiteHelper.ToJsonModelRef(site)
            };

            return Core.Environment.Hal.Apply(Defines.WinAuthResource.Guid, obj);
        }

        public static WindowsAuthenticationSection GetSection(Site site, string path, string configPath = null)
        {
            return (WindowsAuthenticationSection)ManagementUnit.GetConfigSection(site?.Id,
                                                                           path,
                                                                           WindowsAuthenticationSection.SECTION_NAME,
                                                                           typeof(WindowsAuthenticationSection),
                                                                           configPath);
        }

        public static bool IsSectionLocal(Site site, string path)
        {
            return ManagementUnit.IsSectionLocal(site?.Id,
                                                 path,
                                                 WindowsAuthenticationSection.SECTION_NAME);
        }

        public static IEnumerable<object> ProvidersJsonModel(WindowsAuthenticationSection section)
        {
            Dictionary<string, bool> providers = new Dictionary<string,bool>();

            foreach(string provider in section.Providers)
            {
                providers.Add(provider, true);
            }
            foreach(string provider in ProvidersUtil.GetAvailableProvidersList())
            {
                if(!providers.ContainsKey(provider))
                {
                    providers.Add(provider, false);
                }
            }

            return providers.Select(kvp => 
                new {
                    name = kvp.Key,
                    enabled = kvp.Value
                }
            );
        }

        public static string GetLocation(string id)
        {
            return $"{Defines.WIN_AUTH_PATH}/{id}";
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
