// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Microsoft.IIS.Administration.Core;
    using Microsoft.IIS.Administration.Core.Utils;
    using Microsoft.Web.Administration;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO;
    using System.Linq;

    static class OutboundRulesHelper
    {
        public static readonly Fields SectionRefFields = new Fields("id", "scope");
        public static readonly Fields RuleRefFields = new Fields("name", "id");
        public static readonly Fields PreConditionRefFields = new Fields("name", "id");

        public static string GetSectionLocation(string id)
        {
            return $"/{Defines.OUTBOUND_RULES_SECTION_PATH}/{id}";
        }

        public static string GetPreConditionLocation(string id)
        {
            return $"/{Defines.PRECONDITIONS_PATH}/{id}";
        }

        public static string GetRuleLocation(string id)
        {
            return $"/{Defines.OUTBOUND_RULES_PATH}/{id}";
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

            RewriteId id = new RewriteId(site?.Id, path);
            var section = GetSection(site, path);

            dynamic obj = new ExpandoObject();

            //
            // id
            if (fields.Exists("id")) {
                obj.id = id.Uuid;
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

            return Core.Environment.Hal.Apply(Defines.OutboundRulesSectionResource.Guid, obj, full);
        }

        public static OutboundRulesSection GetSection(Site site, string path, string configPath = null)
        {
            return (OutboundRulesSection)ManagementUnit.GetConfigSection(site?.Id,
                                                                           path,
                                                                           Globals.OutboundRulesSectionName,
                                                                           typeof(OutboundRulesSection),
                                                                           configPath);
        }

        public static bool IsSectionLocal(Site site, string path)
        {
            return ManagementUnit.IsSectionLocal(site?.Id,
                                                 path,
                                                 Globals.OutboundRulesSectionName);
        }

        public static object PreConditionToJsonModelRef(PreCondition precondition, Site site, string path, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return PreConditionToJsonModel(precondition, site, path, PreConditionRefFields, false);
            }
            else {
                return PreConditionToJsonModel(precondition, site, path, fields, false);
            }
        }

        public static object PreConditionToJsonModel(PreCondition precondition, Site site, string path, Fields fields = null, bool full = true)
        {
            if (precondition == null) {
                return null;
            }

            if (fields == null) {
                fields = Fields.All;
            }

            dynamic obj = new ExpandoObject();

            //
            // name
            if (fields.Exists("name")) {
                obj.name = precondition.Name;
            }

            //
            // id
            if (fields.Exists("id")) {
                obj.id = new PreConditionId(site?.Id, path, precondition.Name).Uuid;
            }

            //
            // match
            if (fields.Exists("match")) {
                obj.match = precondition.LogicalGrouping == LogicalGrouping.MatchAll ? "all" : "any";
            }

            //
            // pattern_syntax
            if (fields.Exists("pattern_syntax")) {
                obj.pattern_syntax = Enum.GetName(typeof(PatternSyntax), precondition.PatternSyntax).ToLowerInvariant();
        }

            //
            // requirements
            if (fields.Exists("requirements")) {
                obj.requirements = precondition.Conditions.Select(c => new {
                    input = c.Input,
                    pattern = c.Pattern,
                    negate = c.Negate,
                    ignore_case = c.IgnoreCase
                });
            }

            //
            // outbound_rules
            if (fields.Exists("outbound_rules")) {
                obj.outbound_rules = SectionToJsonModelRef(site, path);
            }

            return Core.Environment.Hal.Apply(Defines.PreConditionsResource.Guid, obj, full);
        }

        public static object RuleToJsonModelRef(OutboundRule rule, Site site, string path, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return RuleToJsonModel(rule, site, path, RuleRefFields, false);
            }
            else {
                return RuleToJsonModel(rule, site, path, fields, false);
            }
        }

        public static object RuleToJsonModel(OutboundRule rule, Site site, string path, Fields fields = null, bool full = true)
        {
            if (rule == null) {
                return null;
            }

            if (fields == null) {
                fields = Fields.All;
            }

            var outboundRuleId = new OutboundRuleId(site?.Id, path, rule.Name);
            var section = GetSection(site, path);
            OutboundRuleMatchType matchType = GetMatchType(rule);

            dynamic obj = new ExpandoObject();

            //
            // name
            if (fields.Exists("name")) {
                obj.name = rule.Name;
            }

            //
            // id
            if (fields.Exists("id")) {
                obj.id = outboundRuleId.Uuid;
            }

            // precondition
            if (fields.Exists("precondition")) {
                var precondition = section.PreConditions.FirstOrDefault(pc => pc.Name.Equals(rule.PreCondition, StringComparison.OrdinalIgnoreCase));
                obj.precondition = PreConditionToJsonModelRef(precondition, site, path, fields.Filter("precondition"));
            }

            // match_type
            if (fields.Exists("match_type")) {
                obj.match_type = Enum.GetName(typeof(OutboundRuleMatchType), matchType).ToLower();
            }

            // server_variable
            if (fields.Exists("server_variable") && matchType == OutboundRuleMatchType.ServerVariable) {
                obj.server_variable = string.IsNullOrEmpty(rule.Match.ServerVariable) ? null : rule.Match.ServerVariable;
            }

            // html_tags
            if (fields.Exists("html_tags") && matchType == OutboundRuleMatchType.HtmlTags) {
                obj.html_tags = null;

                if (string.IsNullOrEmpty(rule.Match.ServerVariable)) {
                    obj.html_tags = new ExpandoObject();
                    obj.html_tags.standard = TagsToDict(rule.Match.FilterByTags);
                    obj.html_tags.custom = rule.Match.FilterByTags.HasFlag(FilterByTags.CustomTags) ? rule.Match.CustomTags : null;
                }
            }

            //
            // pattern
            if (fields.Exists("pattern")) {
                obj.pattern = rule.Match.Pattern;
            }

            //
            // pattern_syntax
            if (fields.Exists("pattern_syntax")) {
                obj.pattern_syntax = Enum.GetName(typeof(PatternSyntax), rule.PatternSyntax).ToLowerInvariant();
            }

            //
            // ignore_case
            if (fields.Exists("ignore_case")) {
                obj.ignore_case = rule.Match.IgnoreCase;
            }

            //
            // negate
            if (fields.Exists("negate")) {
                obj.negate = rule.Match.Negate;
            }

            //
            // stop_processing
            if (fields.Exists("stop_processing")) {
                obj.stop_processing = rule.StopProcessing;
            }

            //
            // rewrite_value
            if (fields.Exists("rewrite_value")) {
                obj.rewrite_value = rule.Action.RewriteValue;
            }

            //
            // replace_server_variable
            if (fields.Exists("replace_server_variable") && matchType == OutboundRuleMatchType.ServerVariable) {
                obj.replace_server_variable = rule.Action.ReplaceServerVariable;
            }

            //
            // conditions
            if (fields.Exists("conditions")) {
                obj.conditions = rule.Conditions.Select(c => new {
                    input = c.Input,
                    pattern = c.Pattern,
                    negate = c.Negate,
                    ignore_case = c.IgnoreCase,
                    match_type = Enum.GetName(typeof(MatchType), c.MatchType).ToLower()
                });
            }

            //
            // outbound_rules
            if (fields.Exists("outbound_rules")) {
                obj.outbound_rules = SectionToJsonModelRef(site, path);
            }

            return Core.Environment.Hal.Apply(Defines.OutboundRulesResource.Guid, obj, full);
        }

        public static void UpdateRule(dynamic model, OutboundRule rule, OutboundRulesSection section)
        {
            SetRule(model, rule, section);
        }

        public static OutboundRule CreateRule(dynamic model, OutboundRulesSection section)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            if (string.IsNullOrEmpty(DynamicHelper.Value(model.name))) {
                throw new ApiArgumentException("name");
            }

            if (string.IsNullOrEmpty(DynamicHelper.Value(model.pattern))) {
                throw new ApiArgumentException("pattern");
            }

            if (DynamicHelper.To<PatternSyntax>(model.pattern_syntax) == null) {
                throw new ApiArgumentException("pattern_syntax");
            }

            if (DynamicHelper.To<OutboundRuleMatchType>(model.match_type == null)) {
                throw new ApiArgumentException("match_type");
            }

            var rule = (OutboundRule)section.Rules.CreateElement();

            SetRule(model, rule, section);

            return rule;
        }

        public static void AddRule(OutboundRule rule, OutboundRulesSection section)
        {
            if (rule == null) {
                throw new ArgumentNullException(nameof(rule));
            }

            if (rule.Name == null) {
                throw new ArgumentNullException("rule.Name");
            }

            if (section.Rules.Any(r => r.Name.Equals(rule.Name))) {
                throw new AlreadyExistsException("rule");
            }

            try {
                section.Rules.Add(rule);
            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
        }

        public static void DeleteRule(OutboundRule rule, OutboundRulesSection section)
        {
            if (rule == null) {
                return;
            }

            rule = (OutboundRule)section.Rules.FirstOrDefault(r => r.Name.Equals(rule.Name));

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

        public static void UpdatePreCondition(dynamic model, PreCondition rule, OutboundRulesSection section)
        {
            SetPreCondition(model, rule, section);
        }

        public static PreCondition CreatePreCondition(dynamic model, OutboundRulesSection section)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            if (string.IsNullOrEmpty(DynamicHelper.Value(model.name))) {
                throw new ApiArgumentException("name");
            }

            var precondition = section.PreConditions.CreateElement();

            SetPreCondition(model, precondition, section);

            return precondition;
        }

        public static void AddPreCondition(PreCondition precondition, OutboundRulesSection section)
        {
            if (precondition == null) {
                throw new ArgumentNullException(nameof(precondition));
            }

            if (precondition.Name == null) {
                throw new ArgumentNullException("precondition.Name");
            }

            if (section.PreConditions.Any(r => r.Name.Equals(precondition.Name))) {
                throw new AlreadyExistsException("precondition");
            }

            try {
                section.PreConditions.Add(precondition);
            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
        }

        public static void DeletePreCondition(PreCondition precondition, OutboundRulesSection section)
        {
            if (precondition == null) {
                return;
            }

            precondition = section.PreConditions.FirstOrDefault(r => r.Name.Equals(precondition.Name));

            if (precondition != null) {
                try {
                    section.PreConditions.Remove(precondition);
                }
                catch (FileLoadException e) {
                    throw new LockedException(section.SectionPath, e);
                }
                catch (DirectoryNotFoundException e) {
                    throw new ConfigScopeNotFoundException(e);
                }
            }
        }

        public static RewriteId GetSectionIdFromBody(dynamic model)
        {
            if (model.outbound_rules == null) {
                throw new ApiArgumentException("outbound_rules");
            }

            if (!(model.outbound_rules is JObject)) {
                throw new ApiArgumentException("outbound_rules", ApiArgumentException.EXPECTED_OBJECT);
            }

            string rewriteId = DynamicHelper.Value(model.outbound_rules.id);

            if (rewriteId == null) {
                throw new ApiArgumentException("outbound_rules.id");
            }

            return new RewriteId(rewriteId);
        }

        private static Dictionary<string, bool> TagsToDict(FilterByTags tags)
        {
            Dictionary<string, bool> dict = new Dictionary<string, bool>();
            dict.Add("a", tags.HasFlag(FilterByTags.A));
            dict.Add("area", tags.HasFlag(FilterByTags.Area));
            dict.Add("base", tags.HasFlag(FilterByTags.Base));
            dict.Add("form", tags.HasFlag(FilterByTags.Form));
            dict.Add("frame", tags.HasFlag(FilterByTags.Frame));
            dict.Add("head", tags.HasFlag(FilterByTags.Head));
            dict.Add("iframe", tags.HasFlag(FilterByTags.IFrame));
            dict.Add("img", tags.HasFlag(FilterByTags.Img));
            dict.Add("input", tags.HasFlag(FilterByTags.Input));
            dict.Add("link", tags.HasFlag(FilterByTags.Link));
            dict.Add("script", tags.HasFlag(FilterByTags.Script));

            return dict;
        }

        private static OutboundRuleMatchType GetMatchType(OutboundRule rule)
        {
            return string.IsNullOrEmpty(rule.Match.ServerVariable) ? OutboundRuleMatchType.HtmlTags : OutboundRuleMatchType.ServerVariable;
        }

        private static void SetRule(dynamic model, OutboundRule rule, OutboundRulesSection section)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            //
            // Name, check for already existing name
            string name = DynamicHelper.Value(model.name);
            if (!string.IsNullOrEmpty(name)) {
                if (!name.Equals(rule.Name, StringComparison.OrdinalIgnoreCase) &&
                        section.Rules.Any(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) {
                    throw new AlreadyExistsException("name");
                }

                rule.Name = name;
            }

            DynamicHelper.If((object)model.pattern, v => rule.Match.Pattern = v);
            DynamicHelper.If((object)model.rewrite_value, v => rule.Action.RewriteValue = v);
            DynamicHelper.If<bool>((object)model.ignore_case, v => rule.Match.IgnoreCase = v);
            DynamicHelper.If<bool>((object)model.negate, v => rule.Match.Negate = v);
            DynamicHelper.If<PatternSyntax>((object)model.pattern_syntax, v => rule.PatternSyntax = v);
            DynamicHelper.If<bool>((object)model.stop_processing, v => rule.StopProcessing = v);

            //
            // Server Variable
            DynamicHelper.If((object)model.server_variable, v => rule.Match.ServerVariable = v);
            DynamicHelper.If<bool>((object)model.replace_server_variable, v => rule.Action.ReplaceServerVariable = v);

            //
            // Html Tags
            dynamic htmlTags = null;
            dynamic standardTags = null;

            if (model.html_tags != null) {
                if (!(model.html_tags is JObject)) {
                    throw new ApiArgumentException("html_tags", ApiArgumentException.EXPECTED_OBJECT);
                }
                htmlTags = model.html_tags;
            }

            if (htmlTags != null) {
                standardTags = htmlTags.standard;

                if (standardTags != null && !(standardTags is JObject)) {
                    throw new ApiArgumentException("html_tags.standard", ApiArgumentException.EXPECTED_OBJECT);
                }

                // Set custom tags
                DynamicHelper.If((object)htmlTags.custom, v => {
                    TagsElement targetCustomTags = section.Tags.FirstOrDefault(t => t.Name.Equals(v, StringComparison.OrdinalIgnoreCase));

                    if (!string.IsNullOrEmpty(v) && targetCustomTags == null) {
                        throw new NotFoundException("html_tags.custom");
                    }

                    if (targetCustomTags != null) {
                        rule.Match.FilterByTags |= FilterByTags.CustomTags;
                    }

                    rule.Match.CustomTags = targetCustomTags?.Name;
                });
            }

            // Set standard tags
            if (standardTags != null) {
                FilterByTags ruleTags = rule.Match.FilterByTags;

                DynamicHelper.If<bool>((object)standardTags.a, v => SetTagFlag(ref ruleTags, FilterByTags.A, v));
                DynamicHelper.If<bool>((object)standardTags.area, v => SetTagFlag(ref ruleTags, FilterByTags.Area, v));
                DynamicHelper.If<bool>((object)standardTags.@base, v => SetTagFlag(ref ruleTags, FilterByTags.Base, v));
                DynamicHelper.If<bool>((object)standardTags.form, v => SetTagFlag(ref ruleTags, FilterByTags.Form, v));
                DynamicHelper.If<bool>((object)standardTags.frame, v => SetTagFlag(ref ruleTags, FilterByTags.Frame, v));
                DynamicHelper.If<bool>((object)standardTags.head, v => SetTagFlag(ref ruleTags, FilterByTags.Head, v));
                DynamicHelper.If<bool>((object)standardTags.iframe, v => SetTagFlag(ref ruleTags, FilterByTags.IFrame, v));
                DynamicHelper.If<bool>((object)standardTags.img, v => SetTagFlag(ref ruleTags, FilterByTags.Img, v));
                DynamicHelper.If<bool>((object)standardTags.input, v => SetTagFlag(ref ruleTags, FilterByTags.Input, v));
                DynamicHelper.If<bool>((object)standardTags.link, v => SetTagFlag(ref ruleTags, FilterByTags.Link, v));
                DynamicHelper.If<bool>((object)standardTags.script, v => SetTagFlag(ref ruleTags, FilterByTags.Script, v));

                rule.Match.FilterByTags = ruleTags;
            }

            if (model.precondition != null) {
                dynamic precondition = model.precondition;

                if (!(precondition is JObject)) {
                    throw new ApiArgumentException("precondition", ApiArgumentException.EXPECTED_OBJECT);
                }

                string id = DynamicHelper.Value(precondition.id);

                if (string.IsNullOrEmpty(precondition.id)) {
                    throw new ApiArgumentException("precondition.id");
                }

                PreConditionId preconditionId = new PreConditionId(id);

                PreCondition pc = section.PreConditions.FirstOrDefault(p => p.Name.Equals(preconditionId.Name, StringComparison.OrdinalIgnoreCase));

                if (pc == null) {
                    throw new NotFoundException("precondition.id");
                }

                rule.PreCondition = pc.Name;
            }

            //
            // Conditions
            if (model.conditions != null) {

                IEnumerable<dynamic> conditions = model.conditions as IEnumerable<dynamic>;

                if (conditions == null) {
                    throw new ApiArgumentException("conditions", ApiArgumentException.EXPECTED_ARRAY);
                }

                rule.Conditions.Clear();

                foreach (dynamic condition in conditions) {
                    if (!(condition is JObject)) {
                        throw new ApiArgumentException("server_variables.item");
                    }

                    string input = DynamicHelper.Value(condition.input);

                    if (string.IsNullOrEmpty(input)) {
                        throw new ApiArgumentException("conditions.item.input", "Required");
                    }

                    var con = rule.Conditions.CreateElement();
                    con.Input = input;
                    con.Pattern = DynamicHelper.Value(condition.pattern);
                    con.Negate = DynamicHelper.To<bool>(condition.negate);
                    con.IgnoreCase = DynamicHelper.To<bool>(condition.ignore_case);

                    // Schema only specifies pattern match type for outbound rules
                    con.MatchType = MatchType.Pattern;

                    rule.Conditions.Add(con);
                }
            }

            //
            // Set match type
            OutboundRuleMatchType matchType = DynamicHelper.To<OutboundRuleMatchType>(model.match_type) ?? GetMatchType(rule);

            if (matchType == OutboundRuleMatchType.HtmlTags) {
                rule.Match.ServerVariable = null;
            }
            else {
                rule.Match.FilterByTags = FilterByTags.None;
            }
        }

        private static void SetPreCondition(dynamic model, PreCondition precondition, OutboundRulesSection section)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            string name = DynamicHelper.Value(model.name);
            if (!string.IsNullOrEmpty(name)) {
                if (!name.Equals(precondition.Name, StringComparison.OrdinalIgnoreCase) &&
                        section.PreConditions.Any(pc => pc.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) {
                    throw new AlreadyExistsException("name");
                }

                precondition.Name = name;
            }

            //
            // Match (Logical Grouping)
            DynamicHelper.If((object)model.match, v => {
                switch (v.ToLowerInvariant()) {
                    case "all":
                        precondition.LogicalGrouping = LogicalGrouping.MatchAll;
                        break;
                    case "any":
                        precondition.LogicalGrouping = LogicalGrouping.MatchAny;
                        break;
                    default:
                        throw new ApiArgumentException("match");
                }
            });

            // Pattern Syntax
            DynamicHelper.If<PatternSyntax>((object)model.pattern_syntax, v => precondition.PatternSyntax = v);

            //
            // requirements
            if (model.requirements != null) {

                IEnumerable<dynamic> requirements = model.requirements as IEnumerable<dynamic>;

                if (requirements == null) {
                    throw new ApiArgumentException("requirements", ApiArgumentException.EXPECTED_ARRAY);
                }

                precondition.Conditions.Clear();

                foreach (dynamic requirement in requirements) {
                    if (!(requirement is JObject)) {
                        throw new ApiArgumentException("server_variables.item");
                    }

                    string input = DynamicHelper.Value(requirement.input);

                    if (string.IsNullOrEmpty(input)) {
                        throw new ApiArgumentException("requirements.item.input", "Required");
                    }

                    var req = precondition.Conditions.CreateElement();
                    req.Input = input;
                    req.Pattern = DynamicHelper.Value(requirement.pattern);
                    req.Negate = DynamicHelper.To<bool>(requirement.negate);
                    req.IgnoreCase = DynamicHelper.To<bool>(requirement.ignore_case);

                    // Schema only specifies pattern match type for outbound rules
                    req.MatchType = PreConditionMatchType.Pattern;

                    precondition.Conditions.Add(req);
                }
            }
        }

        private static void SetTagFlag(ref FilterByTags target, FilterByTags flag, bool value)
        {
            if (value) {
                target |= flag;
            }
            else {
                target &= ~flag;
            }
        }
    }
}
