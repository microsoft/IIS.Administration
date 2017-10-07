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
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    class LocationsHelper
    {
        private IFileOptions _options;
        private IConfigurationWriter _writer;

        public LocationsHelper(IFileOptions options, IConfigurationWriter configurationWriter)
        {
            _options = options;
            _writer = configurationWriter;
        }

        public IFileOptions Options {
            get {
                return _options;
            }
        }

        public object ToJsonModel(ILocation location)
        {
            FileId id = FileId.FromPhysicalPath(location.Path);

            var obj = new {
                alias = location.Alias,
                id = id.Uuid,
                path = location.GetRawPath(),
                claims = location.Claims
            };

            return Core.Environment.Hal.Apply(Defines.LocationsResource.Guid, obj);
        }

        public string GetLocationPath(string id)
        {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentNullException(nameof(id));
            }

            return $"/{Defines.LOCATIONS_PATH}/{id}";
        }

        public ILocation CreateLocation(dynamic model)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            string alias = DynamicHelper.Value(model.alias);
            string path = DynamicHelper.Value(model.path);
            IEnumerable<string> claims = null;

            //
            // Validate model
            if (!string.IsNullOrEmpty(alias) && !PathUtil.IsValidFileName(alias)) {
                throw new ApiArgumentException("alias");
            }

            if (string.IsNullOrEmpty(path) && !PathUtil.IsFullPath(System.Environment.ExpandEnvironmentVariables(path))) {
                throw new ApiArgumentException("path");
            }

            if (model.claims == null) {
                throw new ApiArgumentException("claims");
            }

            if (!(model.claims is JArray)) {
                throw new ApiArgumentException("claims", ApiArgumentException.EXPECTED_ARRAY);
            }

            claims = (model.claims as JArray).ToObject<IEnumerable<string>>();

            foreach (string claim in claims) {
                if (!claim.Equals("read", StringComparison.OrdinalIgnoreCase)
                     && !claim.Equals("write", StringComparison.OrdinalIgnoreCase)) {
                    throw new ApiArgumentException("claim");
                }
            }

            ILocation location = new Location() {
                Alias = alias ?? string.Empty,
                Path = path,
                Claims = claims
            };

            CreateIfNecessary(location);

            return location;
        }

        public async Task<ILocation> AddLocation(ILocation location)
        {
            if (!string.IsNullOrEmpty(location.Alias) && _options.Locations.Any(loc => loc.Alias.Equals(location.Alias, StringComparison.OrdinalIgnoreCase))) {
                throw new AlreadyExistsException("alias");
            }

            if (_options.Locations.Any(loc => loc.GetRawPath().Equals(location.GetRawPath(), StringComparison.OrdinalIgnoreCase))) {
                throw new AlreadyExistsException("path");
            }

            List<ILocation> locations = new List<ILocation>();

            locations.AddRange(_options.Locations);
            locations.Add(location);

            await WriteLocations(locations);

            return _options.Locations.FirstOrDefault(loc => ((IRawPath)loc).RawPath.Equals(location.Path));
        }

        public async Task RemoveLocation(ILocation location)
        {
            List<ILocation> locations = new List<ILocation>();

            foreach (var loc in _options.Locations) {
                if (loc != location) {
                    locations.Add(loc);
                }
            }

            await WriteLocations(locations);

            return;
        }

        public async Task<ILocation> UpdateLocation(dynamic model, ILocation location)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            string alias = DynamicHelper.Value(model.alias);
            string path = DynamicHelper.Value(model.path);

            if (!string.IsNullOrEmpty(alias) && !PathUtil.IsValidFileName(alias)) {
                throw new ApiArgumentException("alias");
            }

            if (!string.IsNullOrEmpty(path) && !PathUtil.IsFullPath(System.Environment.ExpandEnvironmentVariables(path))) {
                throw new ApiArgumentException("path");
            }

            IEnumerable<string> claims = null;
            if (model.claims != null) {

                if (!(model.claims is JArray)) {
                    throw new ApiArgumentException("claims", ApiArgumentException.EXPECTED_ARRAY);
                }

                claims = (model.claims as JArray).ToObject<IEnumerable<string>>();

                foreach (string claim in claims) {
                    if (!claim.Equals("read", StringComparison.OrdinalIgnoreCase)
                         && !claim.Equals("write", StringComparison.OrdinalIgnoreCase)) {
                        throw new ApiArgumentException("claim");
                    }
                }
            }

            if (!string.IsNullOrEmpty(alias) && !alias.Equals(location.Alias, StringComparison.OrdinalIgnoreCase) && 
                    _options.Locations.Any(loc => loc.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase))) {

                throw new AlreadyExistsException("alias");
            }

            if (path != null && !path.Equals(location.GetRawPath(), StringComparison.OrdinalIgnoreCase) &&
                    _options.Locations.Any(loc => loc.GetRawPath().Equals(path, StringComparison.OrdinalIgnoreCase))) {

                throw new AlreadyExistsException("path");
            }

            //
            // Claims are read only so we must create a new location
            ILocation newLocation = new Location() {
                Alias = alias ?? location.Alias,
                Path = path ?? location.GetRawPath(),
                Claims = claims ?? location.Claims
            };

            //
            // If path updated create directory
            if (path != null) {
                CreateIfNecessary(newLocation);
            }

            List<ILocation> locations = new List<ILocation>();

            foreach (var loc in _options.Locations) {
                if (loc == location) {
                    //
                    // Replace the old location with the new location
                    locations.Add(newLocation);
                }
                else {
                    locations.Add(loc);
                }
            }

            await WriteLocations(locations);

            //
            // Retreive the new location from the refreshed file options
            return _options.Locations.FirstOrDefault(loc => ((IRawPath)loc).RawPath.Equals(newLocation.Path));
        }

        public async Task WriteLocations(IEnumerable<ILocation> locations)
        {

            _writer.WriteSection("files:locations", locations.Select(loc => ToConfigurationModel(loc)));

            //
            // Wait for update to be performed
            if (_options is IChangeToken) {
                try {
                    await ((IChangeToken)_options).WaitForChange(2000);
                }
                catch (TimeoutException e) {
                    Log.Error(e, "File configuration watcher timed out");
                    // We will return the old settings
                }
            }
        }

        private void CreateIfNecessary(ILocation location)
        {
            if (!location.Claims.Any(claim => claim.Equals("read", StringComparison.OrdinalIgnoreCase) ||
                    claim.Equals("write", StringComparison.OrdinalIgnoreCase))) {
                return;
            }

            string path = System.Environment.ExpandEnvironmentVariables(location.Path);

            try {
                if (!Directory.Exists(path)) {
                    Directory.CreateDirectory(path);
                }
            }
            catch (IOException e) {
                if (e.HResult == HResults.FileNotFound ||
                    e.HResult == HResults.PathNotFound) {
                    throw new NotFoundException("path");
                }

                throw;
            }
            catch (UnauthorizedAccessException) {
                throw new ForbiddenArgumentException(path);
            }
        }

        private object ToConfigurationModel(ILocation location)
        {
            return new {
                alias = location.Alias,
                path = location.GetRawPath(),
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

    static class LocationExtensions
    {
        public static string GetRawPath(this ILocation location)
        {
            return (location as IRawPath)?.RawPath ?? location.Path;
        }
    }
}
