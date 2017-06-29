// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.IPRestrictions
{
    using Core.Utils;
    using Core;
    using System;
    using System.Net;
    using Web.Administration;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json.Linq;
    using Sites;
    using System.IO;
    using System.Dynamic;
    using System.Threading.Tasks;

    static class IPRestrictionsHelper
    {
        public const string FEATURE_NAME = "IIS-IPSecurity";
        public const string MODULE = "IpRestrictionModule";

        private const string DenyActionAttribute = "denyAction";
        private const string EnableProxyModeAttribute = "enableProxyMode";
        private static readonly Fields RuleRefFields = new Fields("id", "ip_address");

        public static void SetFeatureSettings(dynamic model, Site site, string path, string configPath = null)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            var hasDynamic = ManagementUnit.ServerManager.GetApplicationHostConfiguration().HasSection(IPRestrictionsGlobals.DynamicIPSecuritySectionName);

            var mainSection = GetSection(site, path, configPath);
            DynamicIPSecuritySection dynamicSection = null;

            if (hasDynamic) {
                dynamicSection = GetDynamicSecuritySection(site, path, configPath);
            }

            try {

                DynamicHelper.If<bool>((object)model.allow_unlisted, v => mainSection.AllowUnlisted = v);
                DynamicHelper.If<bool>((object)model.enable_reverse_dns, v => mainSection.EnableReverseDns = v);

                if (mainSection.Schema.HasAttribute(EnableProxyModeAttribute)) {
                    DynamicHelper.If<bool>((object)model.enable_proxy_mode, v => mainSection.EnableProxyMode = v);
                }
                if (mainSection.Schema.HasAttribute(DenyActionAttribute)) {
                    DynamicHelper.If<DenyActionType>((object)model.deny_action, v => mainSection.DenyAction = v);
                }

                if (hasDynamic) {
                    DynamicHelper.If<bool>((object)model.logging_only_mode, v => dynamicSection.LoggingOnlyMode = v);

                    // Concurrent request restrictions
                    if (model.deny_by_concurrent_requests != null) {

                        if (!(model.deny_by_concurrent_requests is JObject)) {
                            throw new ApiArgumentException("deny_by_concurrent_requests");
                        }

                        dynamic denyConcurrent = model.deny_by_concurrent_requests;

                        DynamicHelper.If<bool>((object)denyConcurrent.enabled, v => dynamicSection.DenyByConcurrentRequests.Enabled = v);
                        DynamicHelper.If((object)denyConcurrent.max_concurrent_requests, 1, 4294967295, v => dynamicSection.DenyByConcurrentRequests.MaxConcurrentRequests = v);
                    }

                    // Request rate restrictions
                    if (model.deny_by_request_rate != null) {

                        if (!(model.deny_by_request_rate is JObject)) {
                            throw new ApiArgumentException("deny_by_request_rate");
                        }

                        dynamic denyRequestRate = model.deny_by_request_rate;

                        DynamicHelper.If<bool>((object)denyRequestRate.enabled, v => dynamicSection.DenyByRequestRate.Enabled = v);
                        DynamicHelper.If((object)denyRequestRate.max_requests, 1, 4294967295, v => dynamicSection.DenyByRequestRate.MaxRequests = v);
                        DynamicHelper.If((object)denyRequestRate.time_period, 1, 4294967295, v => dynamicSection.DenyByRequestRate.TimePeriod = v);
                    }
                }

                // Enabled
                DynamicHelper.If<bool>((object)model.enabled, v => {
                    if (!v) {
                        mainSection.AllowUnlisted = true;
                        mainSection.EnableReverseDns = false;

                        if (mainSection.Schema.HasAttribute(EnableProxyModeAttribute)) {
                            mainSection.EnableProxyMode = false;
                        }

                        if (hasDynamic) {
                            dynamicSection.DenyByConcurrentRequests.Enabled = false;
                            dynamicSection.DenyByRequestRate.Enabled = false;
                        }

                        mainSection.IpAddressFilters.Clear();
                    }
                });

                if (model.metadata != null) {
                    DynamicHelper.If<OverrideMode>((object)model.metadata.override_mode, v => {
                        mainSection.OverrideMode = v;
                        
                        if (hasDynamic) {
                            dynamicSection.OverrideMode = v;
                        }
                    });
                }
            }
            catch(FileLoadException e) {
                throw new LockedException(mainSection.SectionPath + (hasDynamic ? "|" + dynamicSection.SectionPath : string.Empty), e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
        }

        public static List<Rule> GetRules(Site site, string path, string configPath = null)
        {
            var collection = GetSection(site, path, configPath).IpAddressFilters;
            if (collection != null) {
                return collection.ToList();
            }
            return new List<Rule>();
        }

        public static Rule CreateRule(dynamic model, IPSecuritySection section)
        {
            if(model == null) {
                throw new ApiArgumentException("model");
            }

            IPAddress ip = null;
            string ipAddress = DynamicHelper.Value(model.ip_address);
            if (string.IsNullOrEmpty(ipAddress)) {
                throw new ApiArgumentException("ip_address");
            }

            try {
                // Throw format error if ip address is not a valid ip address
                ip = IPAddress.Parse(ipAddress);
            }
            catch(FormatException e) {
                throw new ApiArgumentException("ip_address", e);
            }

            if (model.allowed == null) {
                throw new ApiArgumentException("allowed");
            }

            Rule rule = section.IpAddressFilters.CreateElement();
            
            rule.IpAddress = ip;

            SetRule(rule, model, section);

            return rule;
        }

        public static Rule SetRule(Rule rule, dynamic model, IPSecuritySection section)
        {
            if(rule == null) {
                throw new ArgumentNullException("rule");
            }

            string subnetMask = DynamicHelper.Value(model.subnet_mask);
            if (subnetMask != null) {

                try {

                    // Throw format exception of subnet mask is not in valid format
                    IPAddress.Parse(subnetMask);
                }
                catch(FormatException e) {
                    throw new ApiArgumentException("subnet_mask", e);
                }
            }

            string inIp = DynamicHelper.Value(model.ip_address);
            IPAddress ipAddress = null;
            if (inIp != null) {

                try {

                    ipAddress = IPAddress.Parse(inIp);
                }
                catch (FormatException e) {
                    throw new ApiArgumentException("ip_address", e);
                }

                if (ipAddress != rule.IpAddress && section.IpAddressFilters.Any(r => r.IpAddress.Equals(ipAddress))) {
                    throw new AlreadyExistsException("ip_address");
                }
            }

            try {
                rule.IpAddress = ipAddress != null ? ipAddress : rule.IpAddress;
                rule.Allowed = DynamicHelper.To<bool>(model.allowed) ?? rule.Allowed;
                rule.SubnetMask = subnetMask ?? rule.SubnetMask;
                rule.DomainName = DynamicHelper.Value(model.domain_name) ?? rule.DomainName;
            }
            catch(FileLoadException e) {
                throw new LockedException(IPRestrictionsGlobals.IPSecuritySectionName, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }

            return rule;
        }

        public static void AddRule(Rule rule, IPSecuritySection section)
        {
            if (rule == null) {
                throw new ArgumentNullException("rule");
            }
            if (rule.IpAddress == null) {
                throw new ArgumentNullException("rule.IpAddress");
            }

            IPAddressFilterCollection collection = section.IpAddressFilters;

            int dummy;
            if (ExistsAddressFilter(collection, rule.DomainName, rule.IpAddress, rule.SubnetMask, out dummy)) {
                throw new AlreadyExistsException("rule");
            }

            try {
                Rule element = collection.Add(rule);
                element.Allowed = rule.Allowed;
            }
            catch(FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
        }

        public static void DeleteRule(Rule rule, IPSecuritySection section)
        {
            IPAddressFilterCollection collection = section.IpAddressFilters;

            int dummy;
            if (ExistsAddressFilter(collection, rule.DomainName, rule.IpAddress, rule.SubnetMask, out dummy)) {
                try {
                    collection.RemoveAt(dummy);
                }
                catch (FileLoadException e) {
                    throw new LockedException(section.SectionPath, e);
                }
                catch (DirectoryNotFoundException e) {
                    throw new ConfigScopeNotFoundException(e);
                }
            }
        }

        internal static object ToJsonModel(Site site, string path)
        {
            // Dynamic ip security section added in iis 8.0
            var hasDynamic = ManagementUnit.ServerManager.GetApplicationHostConfiguration().HasSection(IPRestrictionsGlobals.DynamicIPSecuritySectionName);

            var section = GetSection(site, path);
            DynamicIPSecuritySection dynamicSection = null;

            bool isLocal;
            bool isLocked;
            bool isEnabled;

            OverrideMode overrideMode;
            OverrideMode overrideModeEffective;

            isLocal = section.IsLocallyStored;
            isLocked = section.IsLocked;
            isEnabled = section.IpAddressFilters.Count != 0
                                    || section.EnableReverseDns
                                    || !section.AllowUnlisted;

            if (section.Schema.HasAttribute(EnableProxyModeAttribute)) {
                isEnabled = isEnabled || section.EnableProxyMode;
            }

            overrideMode = section.OverrideMode;
            overrideModeEffective = section.OverrideModeEffective;


            if (hasDynamic) {
                dynamicSection = GetDynamicSecuritySection(site, path);

                isLocal = isLocal || dynamicSection.IsLocallyStored;
                isLocked = isLocked || dynamicSection.IsLocked;
                isEnabled = isEnabled || dynamicSection.DenyByConcurrentRequests.Enabled
                                      || dynamicSection.DenyByRequestRate.Enabled;

                overrideMode = section.OverrideMode != dynamicSection.OverrideMode ? OverrideMode.Unknown : section.OverrideMode;
                overrideModeEffective = section.OverrideModeEffective != dynamicSection.OverrideModeEffective ? OverrideMode.Unknown : section.OverrideModeEffective;
            }

            // Construct id passing possible site and application associated
            IPRestrictionId ipId = new IPRestrictionId(site?.Id, path, isLocal);

            dynamic obj = new ExpandoObject();
            obj.id = ipId.Uuid;
            obj.scope = site == null ? string.Empty : site.Name + path;
            obj.enabled = isEnabled;
            obj.metadata = ConfigurationUtility.MetadataToJson(isLocal, isLocked, overrideMode, overrideModeEffective);
            obj.allow_unlisted = section.AllowUnlisted;
            obj.enable_reverse_dns = section.EnableReverseDns;

            if (section.Schema.HasAttribute(EnableProxyModeAttribute)) {
                obj.enable_proxy_mode = section.EnableProxyMode;
            }

            if (section.Schema.HasAttribute(DenyActionAttribute)) {
                obj.deny_action = Enum.GetName(typeof(DenyActionType), section.DenyAction);
            }

            if (hasDynamic) {
                obj.deny_by_concurrent_requests = new
                {
                    enabled = dynamicSection.DenyByConcurrentRequests.Enabled,
                    max_concurrent_requests = dynamicSection.DenyByConcurrentRequests.MaxConcurrentRequests
                };
                obj.deny_by_request_rate = new
                {
                    enabled = dynamicSection.DenyByRequestRate.Enabled,
                    max_requests = dynamicSection.DenyByRequestRate.MaxRequests,
                    time_period = dynamicSection.DenyByRequestRate.TimePeriod
                };
                obj.logging_only_mode = dynamicSection.LoggingOnlyMode;
            }
            obj.website = SiteHelper.ToJsonModelRef(site);
            

            return Core.Environment.Hal.Apply(Defines.Resource.Guid, obj);
        }

        public static object ToJsonModelRef(Site site, string path)
        {
            var hasDynamic = ManagementUnit.ServerManager.GetApplicationHostConfiguration().HasSection(IPRestrictionsGlobals.DynamicIPSecuritySectionName);

            var section = GetSection(site, path);
            DynamicIPSecuritySection dynamicSection = null;

            if (hasDynamic) {
                dynamicSection = GetDynamicSecuritySection(site, path);
            }

            bool isLocal;
            if (hasDynamic) {
                isLocal = section.IsLocallyStored || dynamicSection.IsLocallyStored;
            }
            else {
                isLocal = section.IsLocallyStored;
            }

            // Construct id passing possible site and application associated
            IPRestrictionId ipId = new IPRestrictionId(site?.Id, path, isLocal);

            var obj = new {
                id = ipId.Uuid,
                scope = site == null ? string.Empty : site.Name + path
            };

            return Core.Environment.Hal.Apply(Defines.Resource.Guid, obj, false);
        }

        internal static object RuleToJsonModel(Rule rule, Site site, string path, Fields fields = null, bool full = true)
        {
            if (rule == null) {
                return null;
            }

            if (fields == null) {
                fields = Fields.All;
            }

            dynamic obj = new ExpandoObject();

            //
            // id
            obj.id = new RuleId(site?.Id, path, rule.IpAddress.ToString()).Uuid;

            //
            // ip_address
            if (fields.Exists("ip_address")) {
                obj.ip_address = rule.IpAddress.ToString();
            }

            //
            // allowed
            if (fields.Exists("allowed")) {
                obj.allowed = rule.Allowed;
            }

            //
            // subnet_mask
            if (fields.Exists("subnet_mask")) {
                obj.subnet_mask = rule.SubnetMask;
            }

            //
            // domain_name
            if (fields.Exists("domain_name")) {
                obj.domain_name = rule.DomainName;
            }

            //
            // ip_restriction
            if (fields.Exists("ip_restriction")) {
                obj.ip_restriction = ToJsonModelRef(site, path);
            }

            return Core.Environment.Hal.Apply(Defines.RulesResource.Guid, obj, full);
        }

        public static object RuleToJsonModelRef(Rule rule, Site site, string path, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return RuleToJsonModel(rule, site, path, RuleRefFields, false);
            }
            else {
                return RuleToJsonModel(rule, site, path, fields, false);
            }
        }


        public static string GetRuleLocation(string id) {
            return $"/{Defines.RULES_PATH}/{id}";
        }


        private static bool ExistsAddressFilter(IPAddressFilterCollection collection,
                                         string domainName, IPAddress address, string subnetMask,
                                         out int index) {
            // NOTE: Normalize things for the config system
            if (domainName == null) {
                domainName = String.Empty;
            }

            index = -1;

            Rule element = collection.Find(domainName, address, subnetMask);
            if (element != null) {
                index = collection.IndexOf(element);

                return true;
            }

            return false;
        }

        public static IPSecuritySection GetSection(Site site, string path, string configPath = null)
        {
            return (IPSecuritySection)ManagementUnit.GetConfigSection(site?.Id,
                                                                           path,
                                                                           IPRestrictionsGlobals.IPSecuritySectionName,
                                                                           typeof(IPSecuritySection),
                                                                           configPath);
        }

        public static DynamicIPSecuritySection GetDynamicSecuritySection(Site site, string path, string configPath = null) {
            return (DynamicIPSecuritySection)ManagementUnit.GetConfigSection(site?.Id,
                                                                           path,
                                                                           IPRestrictionsGlobals.DynamicIPSecuritySectionName,
                                                                           typeof(DynamicIPSecuritySection),
                                                                           configPath);
        }

        public static bool IsSectionLocal(Site site, string path)
        {
            bool isLocal;

            isLocal = ManagementUnit.IsSectionLocal(site?.Id,
                                                 path,
                                                 IPRestrictionsGlobals.IPSecuritySectionName);

            var hasDynamic = ManagementUnit.ServerManager.GetApplicationHostConfiguration().HasSection(IPRestrictionsGlobals.DynamicIPSecuritySectionName);

            if (hasDynamic) {
                var dynamicSection = ManagementUnit.IsSectionLocal(site?.Id,
                                                     path,
                                                     IPRestrictionsGlobals.DynamicIPSecuritySectionName);

                isLocal = isLocal || dynamicSection;
            }

            return isLocal;
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
                await (enabled ? featureManager.Enable(FEATURE_NAME) : featureManager.Disable(FEATURE_NAME));
            }
        }
    }
}
