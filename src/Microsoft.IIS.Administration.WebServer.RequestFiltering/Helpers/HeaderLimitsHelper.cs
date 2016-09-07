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
    public class HeaderLimitsHelper
    {
        public static List<HeaderLimit> GetHeaderLimits(Site site, string path, string configPath = null)
        {
            // Get request filtering section
            RequestFilteringSection requestFilteringSection = RequestFilteringHelper.GetRequestFilteringSection(site, path, configPath);

            var collection = requestFilteringSection.RequestLimits.HeaderLimits;
            if (collection != null) {
                return collection.ToList();
            }
            return new List<HeaderLimit>();
        }

        public static HeaderLimit CreateHeaderLimit(dynamic model, RequestFilteringSection section)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            string header = DynamicHelper.Value(model.header);
            if (string.IsNullOrEmpty(header)) {
                throw new ApiArgumentException("header");
            }

            HeaderLimit headerLimit = section.RequestLimits.HeaderLimits.CreateElement();
            
            headerLimit.Header = header;

            UpdateHeaderLimit(headerLimit, model);

            return headerLimit;
        }

        public static void AddHeaderLimit(HeaderLimit headerLimit, RequestFilteringSection section)
        {
            if (headerLimit == null) {
                throw new ArgumentNullException("headerLimit");
            }
            if (headerLimit.Header == null) {
                throw new ArgumentNullException("headerLimit.Header");
            }

            HeaderLimitCollection collection = section.RequestLimits.HeaderLimits;

            if (collection.Any(h => h.Header.Equals(headerLimit.Header))) {
                throw new AlreadyExistsException("headerLimit");
            }

            try {
                collection.Add(headerLimit);
            }
            catch (FileLoadException e) {
                throw new LockedException(section.SectionPath, e);
            }
            catch (DirectoryNotFoundException e) {
                throw new ConfigScopeNotFoundException(e);
            }
        }

        public static HeaderLimit UpdateHeaderLimit(HeaderLimit headerLimit, dynamic model)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            if (headerLimit == null) {
                throw new ApiArgumentException("headerLimit");
            }
            string header = DynamicHelper.Value(model.header);
            if(header == string.Empty)
            {
                throw new ApiArgumentException("header");
            }

            try {
                headerLimit.Header = DynamicHelper.Value(model.header) ?? headerLimit.Header;
                headerLimit.SizeLimit = DynamicHelper.To(model.size_limit, 0, uint.MaxValue) ?? headerLimit.SizeLimit;
            }
            catch (FileLoadException e) {
                throw new LockedException(RequestFilteringGlobals.RequestFilteringSectionName, e);
            }

            return headerLimit;

        }

        public static void DeleteHeaderLimit(HeaderLimit headerLimit, RequestFilteringSection section)
        {
            if (headerLimit == null) {
                return;
            }

            HeaderLimitCollection collection = section.RequestLimits.HeaderLimits;

            // To utilize the remove functionality we must pull the element directly from the collection
            headerLimit = collection.FirstOrDefault(h => h.Header.Equals(headerLimit.Header));

            if (headerLimit != null) {
                try {
                    collection.Remove(headerLimit);
                }
                catch (FileLoadException e) {
                    throw new LockedException(section.SectionPath, e);
                }
                catch (DirectoryNotFoundException e) {
                    throw new ConfigScopeNotFoundException(e);
                }
            }
        }

        internal static object ToJsonModel(HeaderLimit headerLimit, Site site, string path)
        {
            if (headerLimit == null) {
                return null;
            }

            HeaderLimitId id = new HeaderLimitId(site?.Id, path, headerLimit.Header);

            var obj = new {
                header = headerLimit.Header,
                id = id.Uuid,
                size_limit = headerLimit.SizeLimit,
                request_filtering = RequestFilteringHelper.ToJsonModelRef(site, path)
            };

            return Core.Environment.Hal.Apply(Defines.HeaderLimitsResource.Guid, obj);
        }

        public static object ToJsonModelRef(HeaderLimit headerLimit, Site site, string path)
        {
            if (headerLimit == null) {
                return null;
            }

            HeaderLimitId id = new HeaderLimitId(site?.Id, path, headerLimit.Header);

            var obj = new {
                header = headerLimit.Header,
                id = id.Uuid,
                size_limit = headerLimit.SizeLimit
            };

            return Core.Environment.Hal.Apply(Defines.HeaderLimitsResource.Guid, obj, false);
        }

        public static string GetLocation(string id) {
            return $"/{Defines.HEADER_LIMITS_PATH}/{id}";
        }
    }
}
