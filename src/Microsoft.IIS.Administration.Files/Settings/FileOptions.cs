// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using Extensions.Configuration;
    using System.Collections.Generic;
    using System.Linq;

    public class FileOptions
    {
        private bool _searchedForAll;
        private AllowedRoot _allPaths;

        private FileOptions() { }

        public List<AllowedRoot> Allowed_Roots { get; set; }

        public AllowedRoot WildCardRoot {
            get {
                if (_searchedForAll) {
                    return _allPaths;
                }

                foreach (var allowedRoot in Allowed_Roots) {
                    if (allowedRoot.Path.Equals("*")) {
                        _allPaths = allowedRoot;
                        _searchedForAll = true;
                    }
                }

                return _allPaths;
            }
        }

        public static FileOptions EmptyOptions()
        {
            return new FileOptions() {
                Allowed_Roots = new List<AllowedRoot>()
            };
        }

        public static FileOptions FromConfiguration(IConfiguration configuration)
        {
            FileOptions options = null;

            if (configuration.GetSection("files").GetChildren().Count() > 0) {
                options = EmptyOptions();
                ConfigurationBinder.Bind(configuration.GetSection("files"), options);
            }

            return options ?? FileOptions.DefaultOptions();
        }

        public static FileOptions DefaultOptions()
        {
            var options = EmptyOptions();

            options.Allowed_Roots.Add(new AllowedRoot() {
                Path = @"%SystemDrive%\inetpub\wwwroot",
                Read_Only = false
            });

            return options;
        }
    }
}
