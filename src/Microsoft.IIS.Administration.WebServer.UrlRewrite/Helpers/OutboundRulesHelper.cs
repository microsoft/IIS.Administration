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
        public static readonly Fields CustomTagsRefFields = new Fields("name", "id");

        public static string GetSectionLocation(string id)
        {
            return $"/{Defines.OUTBOUND_RULES_SECTION_PATH}/{id}";
        }

        public static string GetCustomTagsLocation(string id)
        {
            return $"/{Defines.CUSTOM_TAGS_PATH}/{id}";
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
            // rewrite_before_cache
            if (fields.Exists("rewrite_before_cache")) {
                obj.rewrite_before_cache = section.RewriteBeforeCache;
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

        public static object TagsToJsonModelRef(TagsElement tags, Site site, string path, Fields fields = null)
        {
            if (fields == null || !fields.HasFields)
            {
                return TagsToJsonModel(tags, site, path, CustomTagsRefFields, false);
            }
            else
            {
                return TagsToJsonModel(tags, site, path, fields, false);
            }
        }

        public static object TagsToJsonModel(TagsElement tags, Site site, string path, Fields fields = null, bool full = true)
        {
            if (tags == null)
            {
                return null;
            }

            if (fields == null)
            {
                fields = Fields.All;
            }

            dynamic obj = new ExpandoObject();

            //
            // name
            if (fields.Exists("name"))
            {
                obj.name = tags.Name;
            }

            //
            // id
            if (fields.Exists("id"))
            {
                obj.id = new CustomTagsId(site?.Id, path, tags.Name).Uuid;
            }

            //
            // tags
            if (fields.Exists("tags"))
            {
                obj.tags = tags.Tags.Select(t => new {
                    name = t.Name,
                    attribute = t.Attribute
                });
            }

            //
            // url_rewrite
            if (fields.Exists("url_rewrite")) {
                obj.url_rewrite = RewriteHelper.ToJsonModelRef(site, path, fields.Filter("url_rewrite"));
            }

            return Core.Environment.Hal.Apply(Defines.CustomTagsResource.Guid, obj, full);
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
                obj.pattern_syntax = PatternSyntaxHelper.ToJsonModel(precondition.PatternSyntax);
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
            // url_rewrite
            if (fields.Exists("url_rewrite")) {
                obj.url_rewrite = RewriteHelper.ToJsonModelRef(site, path, fields.Filter("url_rewrite"));
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

            //
            // priority
            if (fields.Exists("priority")) {
                obj.priority = GetSection(site, path).Rules.IndexOf(rule);
            }

            // precondition
            if (fields.Exists("precondition")) {
                var precondition = section.PreConditions.FirstOrDefault(pc => pc.Name.Equals(rule.PreCondition, StringComparison.OrdinalIgnoreCase));
                obj.precondition = PreConditionToJsonModelRef(precondition, site, path, fields.Filter("precondition"));
            }

            // match_type
            if (fields.Exists("match_type")) {
                obj.match_type = OutboundMatchTypeHelper.ToJsonModel(matchType);
            }

            // server_variable
            if (fields.Exists("server_variable") && matchType == OutboundRuleMatchType.ServerVariable) {
                obj.server_variable = string.IsNullOrEmpty(rule.Match.ServerVariable) ? null : rule.Match.ServerVariable;
            }

            // tag_filters
            if (fields.Exists("tag_filters") && matchType == OutboundRuleMatchType.Response) {

                obj.tag_filters = CreateTagsModel(rule.Match.FilterByTags);

                TagsElement customTags = rule.Match.FilterByTags.HasFlag(FilterByTags.CustomTags) ?
                                            section.Tags.FirstOrDefault(t => t.Name.Equals(rule.Match.CustomTags, StringComparison.OrdinalIgnoreCase)) :
                                            null;

                obj.tag_filters.custom = customTags == null ? null : TagsToJsonModelRef(customTags, site, path, fields.Filter("tag_filters.custom"));
            }

            //
            // pattern
            if (fields.Exists("pattern")) {
                obj.pattern = rule.Match.Pattern;
            }

            //
            // pattern_syntax
            if (fields.Exists("pattern_syntax")) {
                obj.pattern_syntax = PatternSyntaxHelper.ToJsonModel(rule.PatternSyntax);
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
            // enabled
            if (fields.Exists("enabled")) {
                obj.enabled = rule.Action.Type == OutboundActionType.Rewrite ? true : false;
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
            // condition_match_constraints
            if (fields.Exists("condition_match_constraints")) {
                obj.condition_match_constraints = LogicalGroupingHelper.ToJsonModel(rule.Conditions.LogicalGrouping);
            }

            //
            // track_all_captures
            if (fields.Exists("track_all_captures")) {
                obj.track_all_captures = rule.Conditions.TrackAllCaptures;
            }

            //
            // conditions
            if (fields.Exists("conditions")) {
                obj.conditions = rule.Conditions.Select(c => new {
                    input = c.Input,
                    pattern = c.Pattern,
                    negate = c.Negate,
                    ignore_case = c.IgnoreCase,
                    match_type = MatchTypeHelper.ToJsonModel(c.MatchType)
                });
            }

            //
            // url_rewrite
            if (fields.Exists("url_rewrite")) {
                obj.url_rewrite = RewriteHelper.ToJsonModelRef(site, path, fields.Filter("url_rewrite"));
            }

            return Core.Environment.Hal.Apply(Defines.OutboundRulesResource.Guid, obj, full);
        }

        public static void UpdateSection(dynamic model, Site site, string path, string configPath = null)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            OutboundRulesSection section = GetSection(site, path, configPath);

            try {
                DynamicHelper.If<bool>((object)model.rewrite_before_cache, v => section.RewriteBeforeCache = v);

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

            if (string.IsNullOrEmpty(DynamicHelper.Value(model.match_type))) {
                throw new ApiArgumentException("match_type");
            }

            var rule = (OutboundRule)section.Rules.CreateElement();

            //
            // Default to rewrite rule
            rule.Action.Type = OutboundActionType.Rewrite;
            rule.PatternSyntax = PatternSyntax.ECMAScript;

            SetRule(model, rule, section);

            return rule;
        }

        public static void AddRule(OutboundRule rule, OutboundRulesSection section, dynamic model)
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

                UpdatePriority(model, rule, section);
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
                throw new AlreadyExistsException("name");
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

        public static void UpdateCustomTags(dynamic model, TagsElement tags, OutboundRulesSection section)
        {
            SetCustomTags(model, tags, section);
        }

        public static TagsElement CreateCustomTags(dynamic model, OutboundRulesSection section)
        {
            if (model == null)
            {
                throw new ApiArgumentException("model");
            }

            if (string.IsNullOrEmpty(DynamicHelper.Value(model.name)))
            {
                throw new ApiArgumentException("name");
            }

            TagsElement tags = section.Tags.CreateElement();

            SetCustomTags(model, tags, section);

            return tags;
        }

        public static void AddCustomTags(TagsElement tags, OutboundRulesSection section)
        {
            if (tags == null)
            {
                throw new ArgumentNullException(nameof(tags));
            }

            if (tags.Name == null)
            {
                throw new ArgumentNullException("tags.Name");
            }

            if (section.PreConditions.Any(r => r.Name.Equals(tags.Name)))
            {
                throw new AlreadyExistsException("name");
            }

            try
            {
                section.Tags.Add(tags);
            }
            catch (FileLoadException e)
            {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e)
            {
                throw new ConfigScopeNotFoundException(e);
            }
        }

        public static void DeleteCustomTags(TagsElement tags, OutboundRulesSection section)
        {
            if (tags == null)
            {
                return;
            }

            tags = section.Tags.FirstOrDefault(t => t.Name.Equals(tags.Name));

            if (tags != null)
            {
                try
                {
                    section.Tags.Remove(tags);
                }
                catch (FileLoadException e)
                {
                    throw new LockedException(section.SectionPath, e);
                }
                catch (DirectoryNotFoundException e)
                {
                    throw new ConfigScopeNotFoundException(e);
                }
            }
        }

        private static dynamic CreateTagsModel(FilterByTags tags)
        {
            dynamic tagObj = new ExpandoObject();

            tagObj.a = tags.HasFlag(FilterByTags.A);
            tagObj.area = tags.HasFlag(FilterByTags.Area);
            tagObj.@base = tags.HasFlag(FilterByTags.Base);
            tagObj.form = tags.HasFlag(FilterByTags.Form);
            tagObj.frame = tags.HasFlag(FilterByTags.Frame);
            tagObj.head = tags.HasFlag(FilterByTags.Head);
            tagObj.iframe = tags.HasFlag(FilterByTags.IFrame);
            tagObj.img = tags.HasFlag(FilterByTags.Img);
            tagObj.input = tags.HasFlag(FilterByTags.Input);
            tagObj.link = tags.HasFlag(FilterByTags.Link);
            tagObj.script = tags.HasFlag(FilterByTags.Script);

            return tagObj;
        }

        private static OutboundRuleMatchType GetMatchType(OutboundRule rule)
        {
            return string.IsNullOrEmpty(rule.Match.ServerVariable) ? OutboundRuleMatchType.Response : OutboundRuleMatchType.ServerVariable;
        }

        private static void SetRule(dynamic model, OutboundRule rule, OutboundRulesSection section)
        {
            try {
                AssignRuleFromModel(model, rule, section);
            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
        }

        private static void AssignRuleFromModel(dynamic model, OutboundRule rule, OutboundRulesSection section)
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
            DynamicHelper.If<bool>((object)model.enabled, v => rule.Action.Type = v ? OutboundActionType.Rewrite : OutboundActionType.None);
            DynamicHelper.If((object)model.rewrite_value, v => rule.Action.RewriteValue = v);
            DynamicHelper.If<bool>((object)model.ignore_case, v => rule.Match.IgnoreCase = v);
            DynamicHelper.If<bool>((object)model.negate, v => rule.Match.Negate = v);
            DynamicHelper.If<bool>((object)model.stop_processing, v => rule.StopProcessing = v);
            DynamicHelper.If((object)model.pattern_syntax, v => rule.PatternSyntax = PatternSyntaxHelper.FromJsonModel(v));

            //
            // Server Variable
            DynamicHelper.If((object)model.server_variable, v => rule.Match.ServerVariable = v);
            DynamicHelper.If<bool>((object)model.replace_server_variable, v => rule.Action.ReplaceServerVariable = v);

            //
            // Html Tags
            dynamic tagFilters = null;
            dynamic customTags = null;

            if (model.tag_filters != null) {
                tagFilters = model.tag_filters;

                if (!(tagFilters is JObject)) {
                    throw new ApiArgumentException("tag_filters", ApiArgumentException.EXPECTED_OBJECT);
                }

                customTags = tagFilters.custom;

                // Clear custom tags
                rule.Match.CustomTags = null;
                rule.Match.FilterByTags &= ~FilterByTags.CustomTags;
            }

            // Set standard tags
            if (tagFilters != null) {
                FilterByTags ruleTags = rule.Match.FilterByTags;

                DynamicHelper.If<bool>((object)tagFilters.a, v => SetTagFlag(ref ruleTags, FilterByTags.A, v));
                DynamicHelper.If<bool>((object)tagFilters.area, v => SetTagFlag(ref ruleTags, FilterByTags.Area, v));
                DynamicHelper.If<bool>((object)tagFilters.@base, v => SetTagFlag(ref ruleTags, FilterByTags.Base, v));
                DynamicHelper.If<bool>((object)tagFilters.form, v => SetTagFlag(ref ruleTags, FilterByTags.Form, v));
                DynamicHelper.If<bool>((object)tagFilters.frame, v => SetTagFlag(ref ruleTags, FilterByTags.Frame, v));
                DynamicHelper.If<bool>((object)tagFilters.head, v => SetTagFlag(ref ruleTags, FilterByTags.Head, v));
                DynamicHelper.If<bool>((object)tagFilters.iframe, v => SetTagFlag(ref ruleTags, FilterByTags.IFrame, v));
                DynamicHelper.If<bool>((object)tagFilters.img, v => SetTagFlag(ref ruleTags, FilterByTags.Img, v));
                DynamicHelper.If<bool>((object)tagFilters.input, v => SetTagFlag(ref ruleTags, FilterByTags.Input, v));
                DynamicHelper.If<bool>((object)tagFilters.link, v => SetTagFlag(ref ruleTags, FilterByTags.Link, v));
                DynamicHelper.If<bool>((object)tagFilters.script, v => SetTagFlag(ref ruleTags, FilterByTags.Script, v));

                rule.Match.FilterByTags = ruleTags;
            }

            // Set custom tags
            if (customTags != null) {
                if (!(customTags is JObject)) {
                    throw new ApiArgumentException("tags.custom", ApiArgumentException.EXPECTED_OBJECT);
                }

                string ctId = DynamicHelper.Value(customTags.id);

                if (string.IsNullOrEmpty(ctId)) {
                    throw new ArgumentException("tags.custom.id", "required");
                }

                TagsElement targetCustomTags = section.Tags.FirstOrDefault(t => t.Name.Equals(new CustomTagsId(ctId).Name, StringComparison.OrdinalIgnoreCase));

                if (targetCustomTags == null) {
                    throw new NotFoundException("tags.custom");
                }

                rule.Match.FilterByTags |= FilterByTags.CustomTags;
                rule.Match.CustomTags = targetCustomTags.Name;
            }

            if (model.precondition != null) {
                dynamic precondition = model.precondition;

                if (!(precondition is JObject)) {
                    throw new ApiArgumentException("precondition", ApiArgumentException.EXPECTED_OBJECT);
                }

                string id = DynamicHelper.Value(precondition.id);

                if (string.IsNullOrEmpty(id)) {
                    throw new ApiArgumentException("precondition.id");
                }

                PreConditionId preconditionId = new PreConditionId(id);

                PreCondition pc = section.PreConditions.FirstOrDefault(p => p.Name.Equals(preconditionId.Name, StringComparison.OrdinalIgnoreCase));

                if (pc == null) {
                    throw new NotFoundException("precondition.id");
                }

                rule.PreCondition = pc.Name;
            }

            DynamicHelper.If((object)model.condition_match_constraints, v => rule.Conditions.LogicalGrouping = LogicalGroupingHelper.FromJsonModel(v));
            DynamicHelper.If<bool>((object)model.track_all_captures, v => rule.Conditions.TrackAllCaptures = v);

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
            string type = DynamicHelper.Value(model.match_type);
            OutboundRuleMatchType matchType = string.IsNullOrEmpty(type) ? GetMatchType(rule) : OutboundMatchTypeHelper.FromJsonModel(type);

            if (matchType == OutboundRuleMatchType.Response) {
                rule.Match.ServerVariable = null;
            }
            else {
                rule.Match.FilterByTags = FilterByTags.None;
            }

            UpdatePriority(model, rule, section);
        }

        private static void SetPreCondition(dynamic model, PreCondition precondition, OutboundRulesSection section)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            try {
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
                DynamicHelper.If((object)model.pattern_syntax, v => precondition.PatternSyntax = PatternSyntaxHelper.FromJsonModel(v));

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
                            throw new ApiArgumentException("requirements.item");
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
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
        }

        private static void SetCustomTags(dynamic model, TagsElement tagsSet, OutboundRulesSection section)
        {
            if (model == null)
            {
                throw new ApiArgumentException("model");
            }

            try {
                string name = DynamicHelper.Value(model.name);
                if (!string.IsNullOrEmpty(name)) {
                    if (!name.Equals(tagsSet.Name, StringComparison.OrdinalIgnoreCase) &&
                            section.PreConditions.Any(pc => pc.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) {
                        throw new AlreadyExistsException("name");
                    }

                    tagsSet.Name = name;
                }

                //
                // tags
                if (model.tags != null) {

                    IEnumerable<dynamic> tags = model.tags as IEnumerable<dynamic>;

                    if (tags == null) {
                        throw new ApiArgumentException("requirements", ApiArgumentException.EXPECTED_ARRAY);
                    }

                    tagsSet.Tags.Clear();

                    foreach (dynamic requirement in tags) {
                        if (!(requirement is JObject)) {
                            throw new ApiArgumentException("tags.item");
                        }

                        string tagName = DynamicHelper.Value(requirement.name);
                        string attribute = DynamicHelper.Value(requirement.attribute);

                        if (string.IsNullOrEmpty(tagName)) {
                            throw new ApiArgumentException("tags.item.name", "Required");
                        }

                        if (string.IsNullOrEmpty(attribute)) {
                            throw new ApiArgumentException("tags.item.attribute", "Required");
                        }

                        var t = tagsSet.Tags.CreateElement();
                        t.Name = tagName;
                        t.Attribute = attribute;

                        tagsSet.Tags.Add(t);
                    }
                }
            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
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

        private static void UpdatePriority(dynamic model, OutboundRule rule, OutboundRulesSection section)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            DynamicHelper.If((object)model.priority, 0, int.MaxValue, v => {
                v = v >= section.Rules.Count ? section.Rules.Count - 1 : v;
                if (section.Rules.IndexOf(rule) != -1) {
                    section.Rules.Move(rule, (int) v);
                }
            });
        }
    }
}
