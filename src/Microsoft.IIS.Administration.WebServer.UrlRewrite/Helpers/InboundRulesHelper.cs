// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Core.Utils;
    using System;
    using System.Dynamic;
    using System.Linq;
    using Web.Administration;

    static class InboundRulesHelper
    {
        public static readonly Fields SectionRefFields = new Fields("id", "scope");
        public static readonly Fields RuleRefFields = new Fields("name", "id");

        public static string GetSectionLocation(string id)
        {
            return $"/{Defines.INBOUND_RULES_SECTION_PATH}/{id}";
        }

        public static string GetRuleLocation(string id)
        {
            return $"/{Defines.INBOUND_RULES_PATH}/{id}";
        }

        public static object SectionToJsonModelRef(Site site, string path, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return SectionToJsonModel(site, path, SectionRefFields, false);
            }
            else {
                return SectionToJsonModel(site, path, fields, false);
            }
        }

        public static object SectionToJsonModel(Site site, string path, Fields fields = null, bool full = true)
        {
            if (fields == null) {
                fields = Fields.All;
            }

            InboundRulesSectionId inboundRulesId = new InboundRulesSectionId(site?.Id, path);
            var section = GetSection(site, path);

            dynamic obj = new ExpandoObject();

            //
            // id
            if (fields.Exists("id")) {
                obj.id = inboundRulesId.Uuid;
            }

            //
            // scope
            if (fields.Exists("scope")) {
                obj.scope = site == null ? string.Empty : site.Name + path;
            }

            //
            // metadata
            if (fields.Exists("metadata")) {
                obj.metadata = ConfigurationUtility.MetadataToJson(section.IsLocallyStored, section.IsLocked, section.OverrideMode, section.OverrideModeEffective);
            }

            //
            // url_rewrite
            if (fields.Exists("url_rewrite")) {
                obj.url_rewrite = RewriteHelper.ToJsonModelRef(site, path);
            }

            return Core.Environment.Hal.Apply(Defines.InboundRulesSectionResource.Guid, obj, full);
        }

        public static object RuleToJsonModelRef(InboundRuleElement rule, Site site, string path, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return RuleToJsonModel(rule, site, path, RuleRefFields, false);
            }
            else {
                return RuleToJsonModel(rule, site, path, fields, false);
            }
        }

        public static object RuleToJsonModel(InboundRuleElement rule, Site site, string path, Fields fields = null, bool full = true)
        {
            if (rule == null) {
                return null;
            }

            if (fields == null) {
                fields = Fields.All;
            }

            var inboundRuleId = new InboundRuleId(site?.Id, path, rule.Name);

            dynamic obj = new ExpandoObject();

            //
            // name
            if (fields.Exists("name")) {
                obj.name = rule.Name;
            }

            //
            // id
            if (fields.Exists("id")) {
                obj.id = inboundRuleId.Uuid;
            }

            //
            // action
            if (fields.Exists("action")) {
                obj.action = new {
                    type = Enum.GetName(typeof(ActionType), rule.Action.Type).ToLowerInvariant(),
                    url = rule.Action.Url,
                    append_query_string = rule.Action.AppendQueryString,
                    status_code = rule.Action.StatusCode,
                    sub_status_code = rule.Action.SubStatusCode,
                    description = rule.Action.StatusDescription,
                    reason = rule.Action.StatusReason
                };
            }

            //
            // stop_processing
            if (fields.Exists("stop_processing")) {
                obj.stop_processing = rule.StopProcessing;
            }

            //
            // pattern_syntax
            if (fields.Exists("pattern_syntax")) {
                obj.pattern_syntax = Enum.GetName(typeof(PatternSyntax), rule.PatternSyntax).ToLowerInvariant();
            }

            //
            // pattern
            if (fields.Exists("pattern")) {
                obj.pattern = rule.Match.Pattern;
            }

            //
            // negate
            if (fields.Exists("negate")) {
                obj.negate = rule.Match.Negate;
            }

            //
            // sets
            if (fields.Exists("sets")) {
                obj.sets = rule.Sets.Select(s => s.Value);
            }

            //
            // inbound_rules
            if (fields.Exists("inbound_rules")) {
                obj.inbound_rules = SectionToJsonModelRef(site, path);
            }

            return Core.Environment.Hal.Apply(Defines.InboundRulesResource.Guid, obj);
        }

        public static InboundRulesSection GetSection(Site site, string path, string configPath = null)
        {
            return (InboundRulesSection)ManagementUnit.GetConfigSection(site?.Id,
                                                                           path,
                                                                           Globals.RulesSectionName,
                                                                           typeof(InboundRulesSection),
                                                                           configPath);
        }

        public static bool IsSectionLocal(Site site, string path)
        {
            return ManagementUnit.IsSectionLocal(site?.Id,
                                                 path,
                                                 Globals.RulesSectionName);
        }
    }
}

