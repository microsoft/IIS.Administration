// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Authorization
{
    using Core;
    using Core.Utils;
    using Sites;
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Web.Administration;

    static class AuthorizationHelper
    {
        public const string FEATURE_NAME = "IIS-URLAuthorization";
        public const string MODULE = "UrlAuthorizationModule";
        private static readonly Fields RuleRefFields = new Fields("id", "users", "roles", "verbs", "access_type");

        public static List<Rule> GetRules(Site site, string path)
        {
            var section = GetSection(site, path);

            return section.Rules.ToList();
        }

        public static Rule GetRule(Site site, string path, string users, string roles, string verbs)
        {
            var rules = GetRules(site, path);

            return rules.FirstOrDefault(r => r.Users.Equals(users)
                                          && r.Roles.Equals(roles)
                                          && r.Verbs.Equals(verbs));
        }

        public static void UpdateFeatureSettings(dynamic model, AuthorizationSection section)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (section == null) {
                throw new ArgumentNullException("section");
            }

            try {
                DynamicHelper.If<bool>((object)model.bypass_login_pages, v => section.BypassLoginPages = v);
                

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

        public static Rule CreateRule(dynamic model, AuthorizationSection section)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            RuleAccessType? accessType = DynamicHelper.To<RuleAccessType>(model.access_type);
            if(accessType == null) {
                throw new ApiArgumentException("access_type");
            }

            Rule rule = section.Rules.CreateElement();

            SetRule(rule, model);

            return rule;
        }

        public static Rule UpdateRule(Rule rule, dynamic model)
        {
            if (rule == null) {
                throw new ArgumentNullException("rule");
            }
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            SetRule(rule, model);

            return rule;
        }

        public static void DeleteRule(Rule rule, AuthorizationSection section)
        {
            if (rule == null) {
                return;
            }

            rule = section.Rules.FirstOrDefault(r => r.Users.Equals(rule.Users)
                                                  && r.Roles.Equals(rule.Roles)
                                                  && r.Verbs.Equals(rule.Verbs));

            if (rule != null) {
                try {
                    section.Rules.Remove(rule);
                }
                catch (FileLoadException e) {
                    throw new LockedException(section.SectionPath, e);
                }
                catch (DirectoryNotFoundException e) {
                    throw new ConfigScopeNotFoundException(e);
                }
            }
        }

        public static string GetLocation(string id)
        {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentNullException(nameof(id));
            }

            return $"/{Defines.AUTHORIZATION_PATH}/{id}";
        }

        public static string GetRuleLocation(string id)
        {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentNullException(nameof(id));
            }

            return $"/{Defines.RULES_PATH}/{id}";
        }

        internal static object ToJsonModel(Site site, string path)
        {
            var section = GetSection(site, path);

            // Set up metadata, this feature has two configuration sections
            bool isLocal = section.IsLocallyStored;
            bool isLocked = section.IsLocked;
            OverrideMode overrideMode = section.OverrideMode;
            OverrideMode overrideModeEffective = section.OverrideModeEffective;


            AuthorizationId id = new AuthorizationId(site?.Id, path, isLocal);

            var obj = new {
                id = id.Uuid,
                scope = site == null ? string.Empty : site.Name + path,
                metadata = ConfigurationUtility.MetadataToJson(isLocal, isLocked, overrideMode, overrideModeEffective),
                bypass_login_pages = section.BypassLoginPages,
                website = SiteHelper.ToJsonModelRef(site)
            };

            return Core.Environment.Hal.Apply(Defines.AuthorizationResource.Guid, obj);
        }

        public static object ToJsonModelRef(Site site, string path)
        {
            var section = GetSection(site, path);

            // Set up metadata, this feature has two configuration sections
            bool isLocal = section.IsLocallyStored;
            bool isLocked = section.IsLocked;
            OverrideMode overrideMode = section.OverrideMode;
            OverrideMode overrideModeEffective = section.OverrideModeEffective;


            AuthorizationId id = new AuthorizationId(site?.Id, path, isLocal);

            var obj = new {
                id = id.Uuid,
                scope = site == null ? string.Empty : site.Name + path
            };

            return Core.Environment.Hal.Apply(Defines.AuthorizationResource.Guid, obj, false);
        }

        internal static object RuleToJsonModel(Rule rule, Site site, string path, Fields fields = null, bool full = true)
        {
            if (rule == null)
            {
                return null;
            }

            if (fields == null)
            {
                fields = Fields.All;
            }

            dynamic obj = new ExpandoObject();

            //
            // id
            obj.id = new RuleId(site?.Id, path, rule.Users, rule.Roles, rule.Verbs).Uuid;

            //
            // users
            if (fields.Exists("users"))
            {
                obj.users = rule.Users;
            }

            //
            // roles
            if (fields.Exists("roles"))
            {
                obj.roles = rule.Roles;
            }

            //
            // verbs
            if (fields.Exists("verbs"))
            {
                obj.verbs = rule.Verbs;
            }

            //
            // access_type
            if (fields.Exists("access_type"))
            {
                obj.access_type = Enum.GetName(typeof(RuleAccessType), rule.AccessType).ToLower();
            }

            //
            // authorization
            if (fields.Exists("authorization"))
            {
                obj.authorization = ToJsonModelRef(site, path);
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

        private static void SetRule(Rule rule, dynamic model)
        {
            DynamicHelper.If<RuleAccessType>((object)model.access_type, v => rule.AccessType = v);
            DynamicHelper.If((object)model.users, v => rule.Users = v);
            DynamicHelper.If((object)model.roles, v => rule.Roles = v);
            DynamicHelper.If((object)model.verbs, v => rule.Verbs = v);
        }

        internal static AuthorizationSection GetSection(Site site, string path, string configPath = null)
        {
            return (AuthorizationSection)ManagementUnit.GetConfigSection(site?.Id,
                                                                           path,
                                                                           AuthorizationSection.SECTION_PATH,
                                                                           typeof(AuthorizationSection),
                                                                           configPath);
        }

        internal static bool IsSectionLocal(Site site, string path)
        {
            return ManagementUnit.IsSectionLocal(site?.Id,
                                                 path,
                                                 AuthorizationSection.SECTION_PATH);
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
