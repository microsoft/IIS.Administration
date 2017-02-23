// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using Extensions.Configuration;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class FileOptions : IFileOptions
    {
        private List<ILocation> _locations;

        private FileOptions() { }

        public IEnumerable<ILocation> Locations {
            get {
                return _locations;
            }
        }

        public static IFileOptions FromConfiguration(IConfiguration configuration)
        {
            FileOptions options = EmptyOptions();

            if (configuration.GetSection("files").GetChildren().Count() > 0) {
                foreach (var child in configuration.GetSection("files:locations").GetChildren()) {
                    var location = Location.FromSection(child);
                    if (location != null) {
                        options.AddLocation(location);
                    }
                }
            }

            return options;
        }

        private static FileOptions EmptyOptions()
        {
            return new FileOptions()
            {
                _locations = new List<ILocation>()
            };
        }

        public void AddLocation(ILocation location)
        {
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

            _locations.Add(location);

            //
            // Sort
            ((List<ILocation>)Locations).Sort((item1, item2) => {
                return item2.Path.Length - item1.Path.Length;
            });
        }
    }
}
