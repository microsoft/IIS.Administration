// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using Core;
    using Extensions.Configuration;
    using System;
    using System.IO;
    using System.Linq;

    public class AccessControl : IAccessControl
    {
        private FileOptions _options;
        private IConfiguration _configuration;

        internal AccessControl(IConfiguration configuration)
        {
            if (configuration == null) {
                throw new ArgumentNullException(nameof(configuration));
            }

            _configuration = configuration;
        }

        public static IAccessControl Default { get; } = new AccessControl(ConfigurationHelper.Configuration);

        private FileOptions Options
        {
            get {
                if (_options == null) {
                    FileOptions options = FileOptions.FromConfiguration(_configuration);

                    for (var i = 0; i < options.Roots.Count; i++) {
                        options.Roots[i].Path = PathUtil.GetFullPath(options.Roots[i].Path);
                    }

                    // Sort
                    options.Roots.Sort((item1, item2) => {
                        return item1.Path.Length - item2.Path.Length;                        
                    });

                    _options = options;
                }

                return _options;
            }
        }

        public FileAccess GetFileAccess(string path)
        {
            var absolutePath = PathUtil.GetFullPath(path);
            FileAccess allowedAccess = 0;

            //
            // Path must be absolute with no environment variables
            if (!absolutePath.Equals(path, StringComparison.OrdinalIgnoreCase)) {
                return allowedAccess;
            }

            //
            // Best match
            foreach (var root in Options.Roots) {
                if (PathStartsWith(absolutePath, root.Path)) {

                    if (root.Permissions.Any(p => p.Equals("read", StringComparison.OrdinalIgnoreCase))) {
                        allowedAccess |= FileAccess.Read;
                    }
                    if (root.Permissions.Any(p => p.Equals("write", StringComparison.OrdinalIgnoreCase))) {
                        allowedAccess |= FileAccess.Write;
                    }
                }
            }

            return allowedAccess;
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