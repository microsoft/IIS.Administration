// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering
{
    using Core;
    using Core.Utils;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Web.Administration;

    public class UrlsHelper
    {
        public static List<UrlRule> GetUrls(Site site, string path)
        {
            // Get request filtering section
            RequestFilteringSection requestFilteringSection = RequestFilteringHelper.GetRequestFilteringSection(site, path);


            // Consolidates the underlying allow query strings and deny query strings into a single collection
            List<UrlRule> urls = new List<UrlRule>();

            var allowedCollection = requestFilteringSection.AlwaysAllowedUrls;
            if (allowedCollection != null) {
                allowedCollection.ToList().ForEach(u => urls.Add(new UrlRule() {
                    Url = u.Url.TrimStart(new char[] { '/' } ),
                    Allow = true
                }));
            }

            var deniedCollection = requestFilteringSection.DenyUrlSequences;
            if (deniedCollection != null) {
                deniedCollection.ToList().ForEach(u => urls.Add(new UrlRule() {
                    Url = u.Sequence,
                    Allow = false
                }));
            }

            return urls;
        }

        public static UrlRule CreateUrl(dynamic model, RequestFilteringSection section)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            string urlString = DynamicHelper.Value(model.url);
            if (string.IsNullOrEmpty(urlString)) {
                throw new ApiArgumentException("url");
            }
            if(DynamicHelper.To<bool>(model.allow) == null) {
                throw new ApiArgumentException("allow");
            }
            bool allow = DynamicHelper.To<bool>(model.allow);

            return new UrlRule() {
                Url = urlString,
                Allow = allow
            };
        }

        public static void AddUrl(UrlRule url, RequestFilteringSection section)
        {
            if (url == null) {
                throw new ArgumentNullException("rule");
            }
            if (url.Url == null) {
                throw new ArgumentNullException("rule.Url");
            }

            try {
                if (url.Allow) {
                    var collection = section.AlwaysAllowedUrls;
                    AlwaysAllowedUrl allowUrl = collection.CreateElement();

                    if (collection.Any(u => u.Url.Equals(url.Url))) {
                        throw new AlreadyExistsException("url");
                    }

                    allowUrl.Url = url.Url;

                    collection.Add(allowUrl);
                }
                else {
                    var collection = section.DenyUrlSequences;
                    DenyUrlSequence denySequence = collection.CreateElement();

                    if (collection.Any(u => u.Sequence.Equals(url.Url))) {
                        throw new AlreadyExistsException("url");
                    }

                    denySequence.Sequence = url.Url;

                    collection.Add(denySequence);
                }
            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
        }
        public static void UpdateUrl(UrlRule url, dynamic model, Site site, string path, string configPath = null)
        {
            if (url == null) {
                throw new ArgumentNullException("url");
            }
            if (url.Url == null) {
                throw new ArgumentNullException("url.Url");
            }
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            bool allow = DynamicHelper.To<bool>(model.allow) ?? url.Allow;
            string newUrl = DynamicHelper.Value(model.url);

            var section = RequestFilteringHelper.GetRequestFilteringSection(site, path, configPath);

            // Url is in as an allow url
            if (url.Allow) {
                AlwaysAllowedUrl targetUrl = section.AlwaysAllowedUrls.FirstOrDefault(u => u.Url.Equals(url.Url, StringComparison.OrdinalIgnoreCase));

                if(targetUrl == null) {
                    throw new NotFoundException("url");
                }

                section.AlwaysAllowedUrls.Remove(targetUrl);
            }
            // Url is in the configuration as a deny url sequence
            else {
                DenyUrlSequence denySequence = section.DenyUrlSequences.FirstOrDefault(u => u.Sequence.Equals(url.Url, StringComparison.OrdinalIgnoreCase));

                if (denySequence == null) {
                    throw new NotFoundException("url");
                }

                section.DenyUrlSequences.Remove(denySequence);
            }

            try {

                // The target url has been removed from either allow or deny collection.
                // Add updated url to proper collection

                if (allow) {
                    var elem = section.AlwaysAllowedUrls.CreateElement();
                    elem.Url = newUrl ?? url.Url;

                    section.AlwaysAllowedUrls.Add(elem);
                    url.Allow = true;
                }

                else {
                    var elem = section.DenyUrlSequences.CreateElement();
                    elem.Sequence = newUrl ?? url.Url;

                    section.DenyUrlSequences.Add(elem);
                    url.Allow = false;
                }

                url.Url = newUrl ?? url.Url;
            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
        }

        public static void DeleteUrl(UrlRule url, RequestFilteringSection section)
        {
            if (url == null) {
                return;
            }

            try {

                if (url.Allow) {
                    var target = section.AlwaysAllowedUrls.FirstOrDefault(u => u.Url.Equals(url.Url, StringComparison.OrdinalIgnoreCase));

                    if(target != null) {
                        section.AlwaysAllowedUrls.Remove(target);
                    }
                }

                else {
                    var target = section.DenyUrlSequences.FirstOrDefault(u => u.Sequence.Equals(url.Url, StringComparison.OrdinalIgnoreCase));

                    if (target != null) {
                        section.DenyUrlSequences.Remove(target);
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

        internal static object ToJsonModel(UrlRule url, Site site, string path)
        {
            if (url == null) {
                return null;
            }

            UrlId urlId = new UrlId(site?.Id, path, url.Url);

            var obj = new {
                url = url.Url,
                id = urlId.Uuid,
                allow = url.Allow,
                request_filtering = RequestFilteringHelper.ToJsonModelRef(site, path)
            };

            return Core.Environment.Hal.Apply(Defines.UrlsResource.Guid, obj);
        }

        public static object ToJsonModelRef(UrlRule url, Site site, string path)
        {
            if (url == null) {
                return null;
            }

            UrlId urlId = new UrlId(site?.Id, path, url.Url);

            var obj = new {
                url = url.Url,
                id = urlId.Uuid,
                allow = url.Allow
            };

            return Core.Environment.Hal.Apply(Defines.UrlsResource.Guid, obj, false);
        }

        public static string GetLocation(string id) {
            return $"/{Defines.URLS_PATH}/{id}";
        }
    }
}
