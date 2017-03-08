// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using System;
    using System.Collections.Generic;

    public class AccessControl : IAccessControl
    {
        private IFileOptions _options;

        public AccessControl(IFileOptions options)
        {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }

            _options = options;
        }

        public IEnumerable<string> GetClaims(string path)
        {
            //
            // Path must be absolute with no environment variables
            if (PathUtil.IsFullPath(path)) {
                //
                // Best match
                foreach (var location in _options.Locations) {

                    if (HasPrefix(path, location.Path)) {

                        return location.Claims;
                    }
                }
            }

            return null;
        }



        private static bool HasPrefix(string path, string prefix)
        {
            //
            // Remove trailing separator (if any) from prefix
            if (prefix.Length > 0 && prefix.LastIndexOfAny(PathUtil.SEPARATORS) == prefix.Length - 1) {
                prefix = prefix.Substring(0, prefix.Length - 1);
            }

            if (prefix.Length > path.Length) {
                return false;
            }

            var testParts = path.Split(PathUtil.SEPARATORS);
            var prefixParts = prefix.Split(PathUtil.SEPARATORS);

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