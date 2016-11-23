// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using Core;
    using System;
    using System.IO;

    class AccessControl : IAccessControl
    {
        private FileOptions _options;

        private FileOptions Options
        {
            get {
                if (_options == null) {
                    FileOptions options = FileOptions.FromConfiguration(ConfigurationHelper.Configuration);

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
            var absolutePath = Path.GetFullPath(System.Environment.ExpandEnvironmentVariables(path));

            //
            // Path must be absolute with no environment variables
            if (!absolutePath.Equals(path)) {
                return false;
            }

            //
            // Best match
            foreach (var root in Options.Allowed_Roots) {
                if (absolutePath.StartsWith(root.Path, StringComparison.OrdinalIgnoreCase)) {
                    return !(root.Read_Only && fileAccess != FileAccess.Read);
                }
            }

            //
            // Fall back to wildcard
            if (Options.WildCardRoot != null && (Options.WildCardRoot.Read_Only == false || fileAccess == FileAccess.Read)) {
                return true;
            }

            return false;
        }
    }
}