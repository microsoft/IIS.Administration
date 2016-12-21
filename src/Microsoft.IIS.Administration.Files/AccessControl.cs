// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using Core;
    using Extensions.Configuration;
    using System;
    using System.Collections.Generic;

    public class AccessControl : IAccessControl
    {
        private IFileOptions _options;
        private IConfiguration _configuration;

        private AccessControl(IConfiguration configuration)
        {
            if (configuration == null) {
                throw new ArgumentNullException(nameof(configuration));
            }

            _configuration = configuration;
        }

        public static IAccessControl Default { get; } = new AccessControl(ConfigurationHelper.Configuration);

        private IFileOptions Options
        {
            get {
                if (_options == null) {
                    IFileOptions options = FileOptions.FromConfiguration(_configuration);
                    _options = options;
                }

                return _options;
            }
        }

        public IEnumerable<string> GetClaims(string path)
        {
            var claims = new List<string>();

            //
            // Path must be absolute with no environment variables
            if (!PathUtil.IsFullPath(path)) {
                return claims;
            }

            //
            // Best match
            foreach (var location in Options.Locations) {

                if (HasPrefix(path, location.Path)) {

                    claims = location.Claims;

                    break;
                }
            }

            return claims;
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