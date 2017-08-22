// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using Microsoft.Extensions.Primitives;
    using Microsoft.IIS.Administration.Core;
    using Microsoft.IIS.Administration.Core.Utils;
    using Newtonsoft.Json.Linq;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;
    using System.Threading.Tasks;

    static class SettingsHelper
    {
        public static object ToJsonModel(IFileOptions options)
        {
            dynamic obj = new ExpandoObject();

            obj.locations = options.Locations.Select(location => ToJsonModel(location));

            obj.skip_resolving_symbolic_links = options.SkipResolvingSymbolicLinks;

            return obj;
        }

        public static async Task UpdateSettings(dynamic model, IConfigurationWriter configurationWriter, IFileOptions options)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            List<Location> newLocations = null;
            bool? skipResolvingSymboliclinks = DynamicHelper.To<bool>(model.skip_resolving_symbolic_links);

            if (model.locations != null) {

                if (!(model.locations is JArray)) {
                    throw new ApiArgumentException("model.locations", ApiArgumentException.EXPECTED_ARRAY);
                }

                IEnumerable<JObject> locations = (model.locations as JArray).ToObject<IEnumerable<JObject>>();
                newLocations = new List<Location>();

                foreach (dynamic location in locations) {

                    string alias = DynamicHelper.Value(location.alias);
                    string path = DynamicHelper.Value(location.path);
                    IEnumerable<string> claims = null;

                    if (!string.IsNullOrEmpty(alias) && !PathUtil.IsValidFileName(alias)) {
                        throw new ApiArgumentException("location.alias");
                    }

                    if (string.IsNullOrEmpty(path) || !PathUtil.IsFullPath(System.Environment.ExpandEnvironmentVariables(path))) {
                        throw new ApiArgumentException("location.path");
                    }

                    if (location.claims != null) {

                        if (!(location.claims is JArray)) {
                            throw new ApiArgumentException("location.claims", ApiArgumentException.EXPECTED_ARRAY);
                        }

                        claims = (location.claims as JArray).ToObject<IEnumerable<string>>();

                        foreach (string claim in claims) {
                            if (!claim.Equals("read", StringComparison.OrdinalIgnoreCase)
                                 && !claim.Equals("write", StringComparison.OrdinalIgnoreCase)) {
                                throw new ApiArgumentException("location.claim");
                            }
                        }
                    }

                    newLocations.Add(new Location() {
                        Alias = alias,
                        Path = path,
                        Claims = claims ?? new string[] { }
                    });
                }
            }

            bool written = false;

            if (newLocations != null) {
                written = true;
                configurationWriter.WriteSection("files:locations", newLocations.Select(location => ToJsonModel(location)));
            }

            if (skipResolvingSymboliclinks.HasValue) {
                written = true;
                configurationWriter.WriteSection("files:skip_resolving_symbolic_links", skipResolvingSymboliclinks.Value);
            }

            //
            // Wait for update to be performed
            if (written && options is IChangeToken) {
                try {
                    await ((IChangeToken)options).WaitForChange(2000);
                }
                catch (TimeoutException e) {
                    Log.Error(e, "File configuration watcher timed out");
                    // We will return the old settings
                }
            }
        }

        private static object ToJsonModel(ILocation location)
        {
            return new {
                alias = location.Alias,
                path = (location as IRawPath)?.RawPath ?? location.Path,
                claims = location.Claims
            };
        }

        private class Location : ILocation
        {
            public string Alias { get; set; }
            public string Path { get; set; }
            public IEnumerable<string> Claims { get; set; }
        }
    }
}
