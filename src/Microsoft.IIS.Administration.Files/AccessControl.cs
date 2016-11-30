// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using Extensions.Configuration;
    using System;
    using System.IO;

    class AccessControl : IAccessControl
    {
        private FileOptions _options;
        private IConfiguration _configuration;

        public AccessControl(IConfiguration configuration)
        {
            if (configuration == null) {
                throw new ArgumentNullException(nameof(configuration));
            }

            _configuration = configuration;
        }

        private FileOptions Options
        {
            get {
                if (_options == null) {
                    FileOptions options = FileOptions.FromConfiguration(_configuration);

                    for (var i = 0; i < options.Allowed_Roots.Count; i++) {
                        options.Allowed_Roots[i].Path = Environment.ExpandEnvironmentVariables(options.Allowed_Roots[i].Path);
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
            var absolutePath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path));

            //
            // Path must be absolute with no environment variables
            if (!absolutePath.Equals(path)) {
                return false;
            }

            //
            // Best match
            foreach (var root in Options.Allowed_Roots) {
                if (PathStartsWith(absolutePath, root.Path)) {
                    return !(root.Read_Only && fileAccess != FileAccess.Read);
                }
            }

            return false;
        }



        private bool PathStartsWith(string path, string prefix)
        {
            if (prefix.Length > path.Length) {
                return false;
            }

            var separators = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };

            var testParts = path.Split(separators);
            var prefixParts = prefix.TrimEnd(separators).Split(separators);

            if (prefixParts.Length > testParts.Length) {
                return false;
            }

            for (var i = 0; i < prefixParts.Length; i++) {
                if (!prefixParts[i].Equals(testParts[i], StringComparison.OrdinalIgnoreCase)) {
                    return false;
                }
            }

            return true;
        }
    }
}