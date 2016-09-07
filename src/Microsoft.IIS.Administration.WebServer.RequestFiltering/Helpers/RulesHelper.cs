// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering
{
    using Core;
    using Core.Utils;
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.IO;
    using System.Linq;
    using Web.Administration;

    public class RulesHelper
    {
        private static readonly Fields RefFields = new Fields("name", "id");

        public static List<Rule> GetRules(Site site, string path, string configPath = null)
        {
            // Get request filtering section
            RequestFilteringSection requestFilteringSection = RequestFilteringHelper.GetRequestFilteringSection(site, path, configPath);

            var collection = requestFilteringSection.FilteringRules;
            if (collection != null) {
                return collection.ToList();
            }
            return new List<Rule>();
        }

        public static Rule CreateRule(dynamic model, RequestFilteringSection section)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            string name = DynamicHelper.Value(model.name);
            if (string.IsNullOrEmpty(name)) {
                throw new ApiArgumentException("name");
            }

            Rule rule = section.FilteringRules.CreateElement();
            
            rule.Name = name;

            SetRule(rule, model);

            return rule;
        }

        public static void AddRule(Rule rule, RequestFilteringSection section)
        {
            if (rule == null) {
                throw new ArgumentNullException("rule");
            }
            if (rule.Name == null) {
                throw new ArgumentNullException("rule.Name");
            }

            FilteringRuleCollection collection = section.FilteringRules;

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

        public static Rule UpdateRule(Rule rule, dynamic model)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (rule == null) {
                throw new ArgumentNullException("rule");
            }

            SetRule(rule, model);

            return rule;

        }

        private static void SetRule(Rule rule, dynamic model)
        {
            try {
                rule.Name = DynamicHelper.Value(model.name) ?? rule.Name;
                rule.ScanUrl = DynamicHelper.To<bool>(model.scan_url) ?? rule.ScanUrl;
                rule.ScanQueryString = DynamicHelper.To<bool>(model.scan_query_string) ?? rule.ScanQueryString;

                if (model.headers != null) {
                    IEnumerable<dynamic> headers = (IEnumerable<dynamic>)model.headers;

                    // Clear previous headers
                    rule.ScanHeaders.Clear();

                    // Iterate through all headers provided
                    foreach (dynamic header in headers) {

                        // Extract the header string from the dynamic model
                        string value = DynamicHelper.Value(header);
                        if (!String.IsNullOrEmpty(value)) {

                            // Add the header to the rule
                            rule.ScanHeaders.Add(value);
                        }
                    }
                }

                if (model.file_extensions != null) {
                    IEnumerable<dynamic> appliesTo = (IEnumerable<dynamic>)model.file_extensions;

                    rule.AppliesTo.Clear();

                    foreach (dynamic fileExtension in appliesTo) {

                        string value = DynamicHelper.Value(fileExtension);
                        if (!String.IsNullOrEmpty(value)) {

                            rule.AppliesTo.Add(value);
                        }
                    }
                }

                if (model.deny_strings != null) {
                    IEnumerable<dynamic> denyStrings = (IEnumerable<dynamic>)model.deny_strings;

                    rule.DenyStrings.Clear();

                    foreach (dynamic denyString in denyStrings) {

                        string value = DynamicHelper.Value(denyString);
                        if (!String.IsNullOrEmpty(value)) {

                            rule.DenyStrings.Add(value);
                        }
                    }
                }
            }
            catch (FileLoadException e) {
                throw new LockedException(RequestFilteringGlobals.RequestFilteringSectionName, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
        }

        public static void DeleteRule(Rule rule, RequestFilteringSection section)
        {
            if (rule == null) {
                return;
            }

            FilteringRuleCollection collection = section.FilteringRules;

            // To utilize the remove functionality we must pull the element directly from the collection
            rule = collection.FirstOrDefault(r => r.Name.Equals(rule.Name));

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

        internal static object ToJsonModel(Rule rule, Site site, string path, Fields fields, bool full)
        {
            if (rule == null) {
                return null;
            }

            if (fields == null) {
                fields = Fields.All;
            }

            dynamic obj = new ExpandoObject();

            //
            // name
            if (fields.Exists("name")) {
                obj.name = rule.Name;
            }

            //
            // id
            obj.id = new RuleId(site?.Id, path, rule.Name).Uuid;

            //
            // scan_url
            if (fields.Exists("scan_url")) {
                obj.scan_url = rule.ScanUrl;
            }

            //
            // scan_query_string
            if (fields.Exists("scan_query_string")) {
                obj.scan_query_string = rule.ScanQueryString;
            }

            //
            // headers
            if (fields.Exists("headers")) {
                obj.headers = rule.ScanHeaders.Select(scanHeader => scanHeader.RequestHeader);
            }

            //
            // file_extensions
            if (fields.Exists("file_extensions")) {
                obj.file_extensions = rule.AppliesTo.Select(a => a.FileExtension);
            }

            //
            // deny_strings
            if (fields.Exists("deny_strings")) {
                obj.deny_strings = rule.DenyStrings.Select(denyString => denyString.String);
            }

            //
            // request_filtering
            if (fields.Exists("request_filtering")) {
                obj.request_filtering = RequestFilteringHelper.ToJsonModelRef(site, path);
            }

            return Core.Environment.Hal.Apply(Defines.RulesResource.Guid, obj, full);
        }

        public static object ToJsonModelRef(Rule rule, Site site, string path, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return ToJsonModel(rule, site, path, RefFields, false);
            }
            else {
                return ToJsonModel(rule, site, path, fields, false);
            }
        }

        public static string GetLocation(string id) {
            return $"/{Defines.RULES_PATH}/{id}";
        }
    }
}
