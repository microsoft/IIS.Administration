// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRequestTracing
{
    using Core;
    using Core.Utils;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO;
    using System.Linq;
    using Web.Administration;

    public static class RulesHelper
    {
        private static readonly Fields RefFields = new Fields("path", "id", "trace_definition");

        public static IEnumerable<TraceRule> GetRules(Site site, string path, string configPath = null)
        {
            return Helper.GetTraceFailedRequestsSection(site, path, configPath).TraceRules;
        }

        internal static object ToJsonModel(TraceRule rule, Site site, string path, Fields fields = null, bool full = true)
        {
            if (rule == null) {
                return null;
            }

            RuleId ruleId = new RuleId(site?.Id, path, rule.Path);

            if (fields == null) {
                fields = Fields.All;
            }

            dynamic obj = new ExpandoObject();

            //
            // path
            if (fields.Exists("path")) {
                obj.path = rule.Path;
            }

            //
            // id
            obj.id = new RuleId(site?.Id, path, rule.Path).Uuid;

            //
            // status_codes
            if (fields.Exists("status_codes")) {

                var statusCodes = rule.FailureDefinition.StatusCodes.Split(',')
                    .Select(statusCode => statusCode.Trim())
                    .Where(statusCode => !string.IsNullOrEmpty(statusCode));

                obj.status_codes = statusCodes;
            }

            //
            // min_request_execution_time
            if (fields.Exists("min_request_execution_time")) {

                // 0 turns off the time taken trigger, display this value as int max
                var totalSeconds = rule.FailureDefinition.TimeTaken.TotalSeconds;
                obj.min_request_execution_time = totalSeconds == 0 ? int.MaxValue : totalSeconds;
            }

            //
            // event_severity
            if (fields.Exists("event_severity")) {
                obj.event_severity = Enum.GetName(rule.FailureDefinition.Verbosity.GetType(), rule.FailureDefinition.Verbosity).ToLower();
            }

            //
            // custom_action
            if (fields.Exists("custom_action")) {
                obj.custom_action = new {
                    executable = rule.CustomActionExe,
                    @params = rule.CustomActionParams,
                    trigger_limit = rule.CustomActionTriggerLimit
                };
            }

            //
            // traces
            if (fields.Exists("traces")) {

                // It is possible that a trace rule was created and the provider that was providing the areas was removed
                // We do not want to error out in this case, instead display a null provider               

                obj.traces = rule.TraceAreas.Select(ta => {
                    var areas = ta.Areas.Split(',')
                        .Select(area => area.Trim())
                        .Where(area => !string.IsNullOrEmpty(area));

                    var provider = ProvidersHelper.GetProviders(site, path).Where(p => p.Name.Equals(ta.Provider, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    
                    Dictionary<string, bool> allowedAreas = new Dictionary<string, bool>();
                    if (provider != null) {
                        foreach (var area in provider.Areas) {
                            allowedAreas.Add(area.Name, false);
                        }
                        foreach (var area in areas) {
                            allowedAreas[area] = true;
                        }
                    }

                    return new {
                        allowed_areas = allowedAreas,
                        provider = ProvidersHelper.ToJsonModelRef(provider, site, path),
                        verbosity = Enum.GetName(ta.Verbosity.GetType(), ta.Verbosity).ToLower()
                    };
                });
            }

            //
            // request_tracing
            if (fields.Exists("request_tracing")) {
                obj.request_tracing = Helper.ToJsonModelRef(site, path);
            }

            return Core.Environment.Hal.Apply(Defines.RulesResource.Guid, obj, full);
        }

        public static object ToJsonModelRef(TraceRule rule, Site site, string path, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return ToJsonModel(rule, site, path, RefFields, false);
            }
            else {
                return ToJsonModel(rule, site, path, fields, false);
            }
        }

        public static TraceRule CreateRule(dynamic model, Site site, string path, string configPath = null)
        {            
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            if (string.IsNullOrEmpty(DynamicHelper.Value(model.path))) {
                throw new ApiArgumentException("path");
            }

            var section = Helper.GetTraceFailedRequestsSection(site, path, configPath);

            var rule = section.TraceRules.CreateElement();

            SetRule(rule, model, site, path);

            return rule;
        }

        public static TraceRule UpdateRule(TraceRule rule, dynamic model, Site site, string virtualPath, string configPath = null)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (rule == null) {
                throw new ArgumentNullException("rule");
            }

            var section = Helper.GetTraceFailedRequestsSection(site, virtualPath, configPath);

            string path = DynamicHelper.Value(model.path);
            if (path != null
                && !path.Equals(rule.Path)
                && section.TraceRules.Any(r => r.Path.Equals(path, StringComparison.OrdinalIgnoreCase))) {
                throw new AlreadyExistsException("path");
            }

            try {
                SetRule(rule, model, site, virtualPath);
            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }

            return rule;

        }

        public static object AddRule(TraceRule rule, TraceFailedRequestsSection section)
        {
            if (rule == null) {
                throw new ArgumentNullException("rule");
            }
            if (rule.Path == null) {
                throw new ArgumentNullException("rule.Path");
            }

            if (section.TraceRules.Any(r => r.Path.Equals(rule.Path, StringComparison.OrdinalIgnoreCase))) {
                return new AlreadyExistsException("path");
            }

            try {
                section.TraceRules.Add(rule);
            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }

            return rule;
        }

        public static void DeleteRule(TraceRule rule, TraceFailedRequestsSection section)
        {
            if (rule == null) {
                return;
            }

            var collection = section.TraceRules;

            // To utilize the remove functionality we must pull the element directly from the collection
            rule = collection.FirstOrDefault(r => r.Path.Equals(rule.Path));

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

        public static string GetLocation(string id)
        {
            return $"/{Defines.RULES_PATH}/{id}";
        }

        private static void SetRule(TraceRule rule, dynamic model, Site site, string path)
        {
            DynamicHelper.If ((object)model.path, v => rule.Path = v);

            DynamicHelper.If<long>((object)model.min_request_execution_time, v => {

                // Setting time taken to 0 turns off the time taken trace trigger
                if(v < 1) {
                    throw new ApiArgumentException("min_request_execution_time");
                }
                rule.FailureDefinition.TimeTaken = TimeSpan.FromSeconds(v >= int.MaxValue ? 0 : v);                
            });
            DynamicHelper.If<FailureDefinitionVerbosity>((object)model.event_severity, v => rule.FailureDefinition.Verbosity = v);

            // Status codes
            if (model.status_codes != null) {

                // Check for status codes and ensure proper format. e.g. 101, 102-103, 104
                if (!(model.status_codes is JArray)) {
                    throw new ApiArgumentException("model.status_codes", ApiArgumentException.EXPECTED_ARRAY);
                }

                List<string> statusCodes = new List<string>();
                IEnumerable<string> entries = (model.status_codes as JArray).ToObject<IEnumerable<string>>();
                long l;

                foreach (var entry in entries) {
                    var rangeSplit = entry.Split('-');

                    foreach (var rangeEntry in rangeSplit) {

                        if (!long.TryParse(rangeEntry, out l)) {
                            throw new ApiArgumentException("model.status_codes");
                        }
                    }

                    statusCodes.Add(entry.Trim());
                }

                rule.FailureDefinition.StatusCodes = string.Join(",", statusCodes);
            }

            // Custom action
            if (model.custom_action != null) {
                if (!(model.custom_action is JObject)) {
                    throw new ApiArgumentException("custom_action", ApiArgumentException.EXPECTED_OBJECT);
                }

                dynamic customAction = model.custom_action;

                DynamicHelper.If((object)customAction.executable, v => rule.CustomActionExe = v);
                DynamicHelper.If((object)customAction.@params, v => rule.CustomActionParams = v);
                DynamicHelper.If((object)customAction.trigger_limit, 0, 10000, v => rule.CustomActionTriggerLimit = v);
            }

            if (model.traces != null) {

                if (!(model.traces is JArray)) {
                    throw new ApiArgumentException("traces", ApiArgumentException.EXPECTED_ARRAY);
                }

                IEnumerable<dynamic> traces = model.traces;
                rule.TraceAreas.Clear();

                foreach (dynamic ta in traces) {
                    if (!(ta is JObject)) {
                        throw new ApiArgumentException("traces.item", ApiArgumentException.EXPECTED_OBJECT);
                    }

                    TraceArea traceArea = rule.TraceAreas.CreateElement();

                    //
                    // Ensure provider field is object and the referenced provider exists
                    if (ta.provider == null) {
                        throw new ApiArgumentException("traces.item.provider");
                    }
                    if (!(ta.provider is JObject)) {
                        throw new ApiArgumentException("traces.item.provider", ApiArgumentException.EXPECTED_OBJECT);
                    }
                    string providerUuid = DynamicHelper.Value(ta.provider.id);
                    if (string.IsNullOrEmpty(providerUuid)) {
                        throw new ApiArgumentException("traces.item.provider.id");
                    }

                    var providerName = new ProviderId(providerUuid).Name;
                    var provider = ProvidersHelper.GetProviders(site, path).Where(p => p.Name.Equals(providerName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                    if (provider == null) {
                        throw new NotFoundException("traces.item.provider");
                    }

                    traceArea.Provider = provider.Name;

                    DynamicHelper.If<FailedRequestTracingVerbosity>((object)ta.verbosity, v => traceArea.Verbosity = v);
                    
                    if (ta.allowed_areas != null) {
                        if (!(ta.allowed_areas is JObject)) {
                            throw new ApiArgumentException("traces.allowed_areas", ApiArgumentException.EXPECTED_OBJECT);
                        }

                        Dictionary<string, bool> allowedAreas;

                        try {
                            allowedAreas = (ta.allowed_areas as JObject).ToObject<Dictionary<string, bool>>();
                        }
                        catch (JsonSerializationException e) {
                            throw new ApiArgumentException("traces.allowed_areas", e);
                        }

                        List<string> areas = new List<string>();
                        foreach (var key in allowedAreas.Keys) {

                            // Ensure the provider offers the specified area
                            if (!provider.Areas.Any(a => a.Name.Equals(key))) {
                                throw new ApiArgumentException("traces.allowed_areas." + key);
                            }

                            if (allowedAreas[key]) {
                                areas.Add(key);
                            }
                        }

                        traceArea.Areas = string.Join(",", areas);
                    }

                    rule.TraceAreas.Add(traceArea);
                }
            }
        }
    }
}
