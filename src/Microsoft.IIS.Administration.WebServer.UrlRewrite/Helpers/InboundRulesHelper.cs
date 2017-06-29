// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Core.Utils;
    using Microsoft.IIS.Administration.Core;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO;
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

            return Core.Environment.Hal.Apply(Defines.InboundRulesSectionResource.Guid, obj, full);
        }

        public static object RuleToJsonModelRef(InboundRule rule, Site site, string path, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return RuleToJsonModel(rule, site, path, RuleRefFields, false);
            }
            else {
                return RuleToJsonModel(rule, site, path, fields, false);
            }
        }

        public static object RuleToJsonModel(InboundRule rule, Site site, string path, Fields fields = null, bool full = true)
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
            // server_variables
            if (fields.Exists("server_variables")) {
                obj.server_variables = rule.ServerVariableAssignments.Select(s => new {
                    name = s.Name,
                    value = s.Value,
                    replace = s.Replace
                });
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
            // inbound_rules
            if (fields.Exists("inbound_rules")) {
                obj.inbound_rules = SectionToJsonModelRef(site, path);
            }

            return Core.Environment.Hal.Apply(Defines.InboundRulesResource.Guid, obj);
        }

        public static void UpdateRule(dynamic model, InboundRule rule, InboundRulesSection section)
        {
            SetRule(model, rule, section);
        }

        public static InboundRule CreateRule(dynamic model, InboundRulesSection section)
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

            if (model.action == null) {
                throw new ApiArgumentException("action");
            }

            if (!(model.action is JObject)) {
                throw new ApiArgumentException("action", ApiArgumentException.EXPECTED_OBJECT);
            }

            if (DynamicHelper.To<ActionType>(model.action.type) == null) {
                throw new ApiArgumentException("action.type");
            }

            if (string.IsNullOrEmpty(DynamicHelper.Value(model.action.url))) {
                throw new ApiArgumentException("action.url");
            }

            var rule = (InboundRule)section.InboundRules.CreateElement();

            SetRule(model, rule, section);

            return rule;
        }

        public static void AddRule(InboundRule rule, InboundRulesSection section)
        {
            if (rule == null) {
                throw new ArgumentNullException(nameof(rule));
            }

            if (rule.Name == null) {
                throw new ArgumentNullException("rule.Name");
            }

            InboundRuleCollection collection = section.InboundRules;

            if (collection.Any(r => r.Name.Equals(rule.Name))) {
                throw new AlreadyExistsException("rule");
            }

            try {
                collection.Add(rule);
            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
        }

        public static void DeleteRule(InboundRule rule, InboundRulesSection section)
        {
            if (rule == null) {
                return;
            }

            InboundRuleCollection collection = section.InboundRules;

            // To utilize the remove functionality we must pull the element directly from the collection
            rule = (InboundRule)collection.FirstOrDefault(r => r.Name.Equals(rule.Name));

            if (rule != null) {
                try {
                    collection.Remove(rule);
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
            if (model.inbound_rules == null) {
                throw new ApiArgumentException("inbound_rules");
            }

            if (!(model.inbound_rules is JObject)) {
                throw new ApiArgumentException("inbound_rules", ApiArgumentException.EXPECTED_OBJECT);
            }

            string rewriteId = DynamicHelper.Value(model.inbound_rules.id);

            if (rewriteId == null) {
                throw new ApiArgumentException("inbound_rules.id");
            }

            return new RewriteId(rewriteId);
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

        private static void SetRule(dynamic model, InboundRule rule, InboundRulesSection section)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            //
            // Name, check for already existing name
            string name = DynamicHelper.Value(model.name);
            if (!string.IsNullOrEmpty(name)) {
                if (!name.Equals(rule.Name, StringComparison.OrdinalIgnoreCase) &&
                        section.InboundRules.Any(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) {
                    throw new AlreadyExistsException("name");
                }

                rule.Name = name;
            }

            DynamicHelper.If((object)model.pattern, v => rule.Match.Pattern = v);
            DynamicHelper.If<bool>((object)model.ignore_case, v => rule.Match.IgnoreCase = v);
            DynamicHelper.If<bool>((object)model.negate, v => rule.Match.Negate = v);
            DynamicHelper.If<PatternSyntax>((object)model.pattern_syntax, v => rule.PatternSyntax = v);
            DynamicHelper.If<bool>((object)model.stop_processing, v => rule.StopProcessing = v);

            //
            // Action
            dynamic action = model.action;
            if (action != null) {

                if (!(action is JObject)) {
                    throw new ApiArgumentException("action", ApiArgumentException.EXPECTED_OBJECT);
                }

                DynamicHelper.If<ActionType>((object)action.type, v => rule.Action.Type = v);
                DynamicHelper.If((object)action.url, v => rule.Action.Url = v);
                DynamicHelper.If<bool>((object)action.append_query_string, v => rule.Action.AppendQueryString = v);
                DynamicHelper.If<long>((object)action.status_code, v => rule.Action.StatusCode = v);
                DynamicHelper.If<long>((object)action.sub_status_code, v => rule.Action.SubStatusCode = v);
                DynamicHelper.If((object)action.description, v => rule.Action.StatusDescription = v);
                DynamicHelper.If((object)action.reason, v => rule.Action.StatusReason = v);
            }

            //
            // Server variables
            if (model.server_variables != null) {

                IEnumerable<dynamic> serverVariables = model.server_variables as IEnumerable<dynamic>;

                if (serverVariables == null) {
                    throw new ApiArgumentException("server_variables", ApiArgumentException.EXPECTED_ARRAY);
                }

                rule.ServerVariableAssignments.Clear();

                foreach (dynamic serverVariable in serverVariables) {
                    if (!(serverVariable is JObject)) {
                        throw new ApiArgumentException("server_variables.item");
                    }

                    string svName = DynamicHelper.Value(serverVariable.name);
                    string svValue = DynamicHelper.Value(serverVariable.value);
                    bool svReplace = DynamicHelper.To<bool>(serverVariable.replace) ?? false;

                    if (string.IsNullOrEmpty(svName)) {
                        throw new ApiArgumentException("server_variables.item.name", "Required");
                    }

                    if (string.IsNullOrEmpty(svValue)) {
                        throw new ApiArgumentException("server_variables.item.value", "Required");
                    }

                    var svAssignment = rule.ServerVariableAssignments.CreateElement();
                    svAssignment.Name = svName;
                    svAssignment.Value = svValue;
                    svAssignment.Replace = svReplace;

                    rule.ServerVariableAssignments.Add(svAssignment);
                }
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
                    MatchType? matchType = DynamicHelper.To<MatchType>(condition.match_type);

                    if (string.IsNullOrEmpty(input)) {
                        throw new ApiArgumentException("conditions.item.input", "Required");
                    }

                    if (matchType == null) {
                        throw new ApiArgumentException("conditions.item.match_type", "Required");
                    }

                    var con = rule.Conditions.CreateElement();
                    con.Input = input;
                    con.MatchType = matchType.Value;
                    con.Pattern = DynamicHelper.Value(condition.pattern);
                    con.Negate = DynamicHelper.To<bool>(condition.negate);
                    con.IgnoreCase = DynamicHelper.To<bool>(condition.ignore_case);

                    rule.Conditions.Add(con);
                }
            }
        }
    }
}

