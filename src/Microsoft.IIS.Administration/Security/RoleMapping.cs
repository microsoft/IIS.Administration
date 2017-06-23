// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Security {
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Security.Principal;


    sealed class RoleMapping {
        private IConfiguration _config;
        private Dictionary<string, IEnumerable<string>> _roles = new Dictionary<string, IEnumerable<string>>();

        public RoleMapping(IConfiguration config) {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public bool IsUserInRole(ClaimsPrincipal user, string role) {
            if (string.IsNullOrEmpty(role)) {
                throw new ArgumentNullException(nameof(role));
            }

            IEnumerable<string> roleMappings = GetRoleMappings(role.ToLower());

            foreach (var identity in user.Identities) {
                WindowsIdentity wi = identity as WindowsIdentity;
                WindowsPrincipal wp = null;

                foreach (var entry in roleMappings) {
                    //
                    // Check Identity
                    if (string.Equals(identity.Name, entry, StringComparison.OrdinalIgnoreCase)) {
                        return true;
                    }

                    //
                    // Check Principal
                    if (user.IsInRole(entry)) {
                        return true;
                    }

                    //
                    // Check Windows Principal
                    if (wp == null && wi != null) {
                        wp = new WindowsPrincipal(wi);
                    }

                    if (wp != null && wp.IsInRole(entry)) {
                        return true;
                    }
                }
            }

            return false;
        }

        private IEnumerable<string> GetRoleMappings(string role) {
            IEnumerable<string> identities = null;

            if (_roles.TryGetValue(role, out identities)) {
                return identities;
            }

            //
            // Load identities
            identities = new List<string>();
            ConfigurationBinder.Bind(_config.GetSection("security:users:" + role), identities);

            //
            // Copy on wright
            var roles = new Dictionary<string, IEnumerable<string>>(_roles);

            // Add role identities
            roles[role] = identities;

            //
            // Replace the original
            _roles = roles;

            return identities;
        }
    }
}