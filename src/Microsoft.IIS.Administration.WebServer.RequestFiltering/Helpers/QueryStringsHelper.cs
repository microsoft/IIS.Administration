// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering
{
    using Core;
    using Core.Utils;
    using Web.Administration;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    public class QueryStringsHelper
    {
        public static List<QueryStringRule> GetQueryStrings(Site site, string path)
        {
            RequestFilteringSection requestFilteringSection = RequestFilteringHelper.GetRequestFilteringSection(site, path);

            // Consolidates the underlying allow query strings and deny query strings into a single collection
            List<QueryStringRule> queryStrings = new List<QueryStringRule>();

            var allowedCollection = requestFilteringSection.AlwaysAllowedQueryStrings;
            if (allowedCollection != null) {
                allowedCollection.ToList().ForEach(allowed => queryStrings.Add(new QueryStringRule() {
                    QueryString = allowed.QueryString,
                    Allow = true
                }));
            }

            var deniedCollection = requestFilteringSection.DenyQueryStringSequences;
            if (deniedCollection != null) {
                deniedCollection.ToList().ForEach(allowed => queryStrings.Add(new QueryStringRule() {
                    QueryString = allowed.Sequence,
                    Allow = false
                }));
            }

            return queryStrings;
        }

        public static QueryStringRule CreateQueryString(dynamic model)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            string queryString = DynamicHelper.Value(model.query_string);
            if (string.IsNullOrEmpty(queryString)) {
                throw new ApiArgumentException("query_string");
            }

            QueryStringRule queryStringRule = new QueryStringRule();
            
            queryStringRule.QueryString = queryString;

            queryStringRule.Allow = DynamicHelper.To<bool>(model.allow) ?? default(bool);

            return queryStringRule;
        }

        public static void AddQueryString(QueryStringRule queryString, RequestFilteringSection section)
        {
            if (queryString == null) {
                throw new ArgumentNullException("queryString");
            }
            if (string.IsNullOrEmpty(queryString.QueryString)) {
                throw new ArgumentNullException("queryString.QueryString");
            }

            AlwaysAllowedQueryStringCollection allowCollection = section.AlwaysAllowedQueryStrings;
            DenyQueryStringSequenceCollection denyCollection = section.DenyQueryStringSequences;

            if (allowCollection.Any(s => s.QueryString.Equals(queryString.QueryString))
                || denyCollection.Any(s => s.Sequence.Equals(queryString.QueryString))) {
                throw new AlreadyExistsException("query_string");
            }

            try {
                if (queryString.Allow) {
                    allowCollection.Add(queryString.QueryString);
                }
                else {
                    denyCollection.Add(queryString.QueryString);
                }
            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
        }

        public static void UpdateQueryString(QueryStringRule queryString, dynamic model, Site site, string path, string configPath = null)
        {
            if (queryString == null) {
                throw new ArgumentNullException("queryString");
            }
            if (queryString.QueryString == null) {
                throw new ArgumentNullException("queryString.QueryString");
            }
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            bool? allow = DynamicHelper.To<bool>(model.allow);
            string queryStringName = DynamicHelper.Value(model.query_string);

            // Empty change set
            if(string.IsNullOrEmpty(queryStringName) && allow == null) {
                return;
            }

            var section = RequestFilteringHelper.GetRequestFilteringSection(site, path, configPath);

            try {
                // Remove the old query string

                if (queryString.Allow) {

                    // We have to retrieve the configuration element from the allow collection
                    var allowCollection = section.AlwaysAllowedQueryStrings;
                    var allowElement = allowCollection.First(s => s.QueryString.Equals(queryString.QueryString));

                    // Remove the query string from the allow collection
                    allowCollection.Remove(allowElement);
                }
                else {
                    var denyCollection = section.DenyQueryStringSequences;
                    var denyElement = denyCollection.First(s => s.Sequence.Equals(queryString.QueryString));

                    denyCollection.Remove(denyElement);
                }
            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
            

            // Update the query string to its new state
            queryString.Allow = allow == null ? queryString.Allow : allow.Value;
            queryString.QueryString = string.IsNullOrEmpty(queryStringName) ? queryString.QueryString : queryStringName;


            try {
                // Add the updated query string back into its respective collection

                if (queryString.Allow) {
                    // Insert the query string into the allow collection
                    section.AlwaysAllowedQueryStrings.Add(queryString.QueryString);
                }
                else {
                    // Insert the query string into the deny collection
                    section.DenyQueryStringSequences.Add(queryString.QueryString);
                }
            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
        }

        public static void DeleteQueryString(QueryStringRule queryString, RequestFilteringSection section)
        {
            if (string.IsNullOrEmpty(queryString.QueryString)) {
                throw new ArgumentNullException("queryString.QueryString");
            }
            if (queryString == null) {
                return;
            }

            if (queryString.Allow) {
                var collection = section.AlwaysAllowedQueryStrings;

                var elem = collection.FirstOrDefault(s => s.QueryString.Equals(queryString.QueryString));
                if (elem != null) {
                    try {
                        collection.Remove(elem);
                    }
                    catch (FileLoadException e) {
                        throw new LockedException(section.SectionPath, e);
                    }
                    catch (DirectoryNotFoundException e) {
                        throw new ConfigScopeNotFoundException(e);
                    }
                }
            }
            else {
                var collection = section.DenyQueryStringSequences;

                var elem = collection.FirstOrDefault(s => s.Sequence.Equals(queryString.QueryString));
                if (elem != null) {
                    try {
                        collection.Remove(elem);
                    }
                    catch (FileLoadException e) {
                        throw new LockedException(section.SectionPath, e);
                    }
                    catch (DirectoryNotFoundException e) {
                        throw new ConfigScopeNotFoundException(e);
                    }
                }

            }
        }

        public static object ToJsonModelRef(QueryStringRule queryString, Site site, string path)
        {
            if (queryString == null) {
                return null;
            }

            QueryStringId id = new QueryStringId(site?.Id, path, queryString.QueryString);

            var obj = new {
                query_string = queryString.QueryString,
                id = id.Uuid,
                allow = queryString.Allow
            };

            return Core.Environment.Hal.Apply(Defines.QueryStringResource.Guid, obj, false);
        }

        internal static object ToJsonModel(QueryStringRule queryString, Site site, string path)
        {
            if (queryString == null) {
                return null;
            }

            QueryStringId id = new QueryStringId(site?.Id, path, queryString.QueryString);

            var obj = new {
                query_string = queryString.QueryString,
                id = id.Uuid,
                allow = queryString.Allow,
                request_filtering = RequestFilteringHelper.ToJsonModelRef(site, path)
            };

            return Core.Environment.Hal.Apply(Defines.QueryStringResource.Guid, obj);
        }

        public static string GetLocation(string id) {
            return $"/{Defines.QUERY_STRING_PATH}/{id}";
        }
    }
}
