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
        private FileOptions() { }

        public IList<ILocation> Locations { get; set; }

        public static IFileOptions FromConfiguration(IConfiguration configuration)
        {
            FileOptions options = null;

            if (configuration.GetSection("files").GetChildren().Count() > 0) {
                options = EmptyOptions();

                foreach (var child in configuration.GetSection("files:locations").GetChildren()) {
                    var location = Location.FromSection(child);
                    if (location != null) {
                        options.Locations.Add(location);
                    }
                }
            }

            if (options != null) {
                options.InitializeRoots();
            }

            return options ?? DefaultOptions();
        }

        private static FileOptions DefaultOptions()
        {
            var options = EmptyOptions();

            options.Locations.Add(new Location() {
                Path = @"%SystemDrive%\inetpub",
                Claims = new List<string> {
                    "read"
                }
            });

            options.InitializeRoots();

            return options;
        }

        private static FileOptions EmptyOptions()
        {
            return new FileOptions()
            {
                Locations = new List<ILocation>()
            };
        }

        private void InitializeRoots()
        {
            //
            // Expand
            foreach (var location in this.Locations) {
                try {
                    var p = PathUtil.GetFullPath(location.Path);
                    location.Path = p;
                }
                catch (ArgumentException e) {
                    Log.Error(e, $"Invalid path '{location.Path}' in file options.");
                    throw;
                }
            }

            //
            // Proper aliases
            foreach (var location in this.Locations) {
                try {
                    if (!string.IsNullOrEmpty(location.Alias) && !PathUtil.IsValidFileName(location.Alias)) {
                        throw new FormatException("Invalid file name.");
                    }
                }
                catch (FormatException e) {
                    Log.Error(e, $"Invalid alias '{location.Alias}' in file options.");
                    throw;
                }
            }

            //
            // Sort
            ((List<ILocation>)Locations).Sort((item1, item2) => {
                return item2.Path.Length - item1.Path.Length;
            });
        }
    }
}
