// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using Core;
    using Extensions.Configuration;
    using System;
    using System.IO;

    class AccessControl
    {
        private FileOptions _options;

        private FileOptions Options
        {
            get {
                if (_options == null) {
                    var options = FileOptions.EmptyOptions();
                    ConfigurationBinder.Bind(ConfigurationHelper.Configuration.GetSection("files"), options);

                    for (var i = 0; i < options.Allowed_Roots.Count; i++) {
                        options.Allowed_Roots[i].Path = System.Environment.ExpandEnvironmentVariables(options.Allowed_Roots[i].Path);
                    }

                    // Sort
                    options.Allowed_Roots.Sort((item1, item2) => {
                        return item1.Path.Length - item2.Path.Length;                        
                    });

                    _options = options;
                }

                return _options;
            }
        }

        public bool IsAccessAllowed(string path, FileAccess fileAccess)
        {
            if (Options.WildCardRoot != null && (Options.WildCardRoot.Read_Only == false || fileAccess == FileAccess.Read)) {
                return true;
            }

            path = Path.GetFullPath(System.Environment.ExpandEnvironmentVariables(path));

            foreach (var root in _options.Allowed_Roots) {
                if (!(root.Read_Only && fileAccess != FileAccess.Read) && path.StartsWith(root.Path, StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
            }

            return false;
        }
    }
}