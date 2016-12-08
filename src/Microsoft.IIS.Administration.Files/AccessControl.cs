// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using Core;
    using Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

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
            var absolutePath = PathUtil.GetFullPath(path);
            var claims = new List<string>();

            //
            // Path must be absolute with no environment variables
            if (!absolutePath.Equals(path, StringComparison.OrdinalIgnoreCase)) {
                return claims;
            }

            //
            // Best match
            foreach (var location in Options.Locations) {

                if (PathUtil.PathStartsWith(absolutePath, location.Path)) {

                    claims = location.Claims;

                    break;
                }
            }

            return claims;
        }
    }
}