// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using Extensions.Configuration;
    using Microsoft.Extensions.Primitives;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class FileOptions : IFileOptions, IChangeToken
    {
        private ConfigurationReloadToken _changeToken = new ConfigurationReloadToken();
        private List<ILocation> _locations;

        private FileOptions() { }

        public IEnumerable<ILocation> Locations {
            get {
                return _locations;
            }
        }

        public bool SkipResolvingSymbolicLinks { get; private set; }

        public static IFileOptions FromConfiguration(IConfiguration configuration)
        {
            FileOptions options = new FileOptions() {
                _locations = new List<ILocation>()
            };

            options.Set(configuration);

            configuration.GetReloadToken().RegisterChangeCallback(_ => {
                options.HandleConfigurationChange(configuration);
            }, null);
            
            return options;
        }

        public void AddLocation(ILocation location)
        {
            string rawPath = (location as IRawPath)?.RawPath ?? location.Path;

            try {
                var p = PathUtil.GetFullPath(location.Path);
                location.Path = p;

                if (!string.IsNullOrEmpty(location.Alias) && !PathUtil.IsValidFileName(location.Alias)) {
                    throw new FormatException("Invalid file name.");
                }
            }
            catch (ArgumentException e) {
                Log.Error(e, $"Invalid path '{location.Path}' in file options.");
                throw;
            }
            catch (FormatException e) {
                Log.Error(e, $"Invalid alias '{location.Alias}' in file options.");
                throw;
            }

            //
            // Only add the location if it doesn't exist
            if (!_locations.Any(loc => loc.Path.ToLowerInvariant().TrimEnd(PathUtil.SEPARATORS)
                                            .Equals(location.Path.ToLowerInvariant().TrimEnd(PathUtil.SEPARATORS)))) {

                Location resolved = Location.Clone(location);
                resolved.RawPath = rawPath;
                _locations.Add(resolved);
            }

            //
            // Sort
            ((List<ILocation>)Locations).Sort((item1, item2) => {
                return item2.Path.Length - item1.Path.Length;
            });
        }

        #region IChangeToken
        public bool HasChanged {
            get {
                return _changeToken.HasChanged;
            }
        }

        public bool ActiveChangeCallbacks {
            get {
                return _changeToken.ActiveChangeCallbacks;
            }
        }

        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            return this._changeToken.RegisterChangeCallback(callback, state);
        }

        #endregion

        private void Set(IConfiguration configuration)
        {
            _locations.Clear();

            if (configuration.GetSection("files").GetChildren().Count() > 0) {
                foreach (var child in configuration.GetSection("files:locations").GetChildren()) {
                    var location = Location.FromSection(child);
                    if (location != null) {
                        AddLocation(location);
                    }
                }

                SkipResolvingSymbolicLinks = configuration.GetSection("files").GetValue("skip_resolving_symbolic_links", false);
            }
        }

        private void HandleConfigurationChange(IConfiguration configuration)
        {
            Set(configuration);
            configuration.GetReloadToken().RegisterChangeCallback(_ => {
                HandleConfigurationChange(configuration);
            }, null);

            _changeToken.OnReload();
            _changeToken = new ConfigurationReloadToken();
        }
    }
}
