// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Security.Authorization {
    using Microsoft.Extensions.Configuration;


    sealed class AccessPolicyOptions {
        private AccessPolicy _api;
        private AccessPolicy _apiKeys;
        private AccessPolicy _system;
        private IConfigurationSection _section;


        public AccessPolicyOptions(IConfiguration config) {
            _section = config?.GetSection("security:access_policy") ?? null;
        }

        public AccessPolicy Api {
            get {
                if (_api == null) {
                    AccessPolicy policy = GetPolicyFromConfig("api");
                    if (!policy.Forbidden) {
                        policy.AccessKey = true;

                        if (string.IsNullOrEmpty(policy.Users)) {
                            policy.Users = "administrators";
                        }
                    }

                    _api = policy;
                }

                return _api;
            }
        }
        public AccessPolicy ApiKeys {
            get {
                if (_apiKeys == null) {
                    AccessPolicy policy = GetPolicyFromConfig("api_keys");

                    if (!policy.Forbidden) {
                        if (string.IsNullOrEmpty(policy.Users)) {
                            policy.Users = "administrators";
                        }
                    }

                    _apiKeys = policy;
                }

                return _apiKeys;
            }
        }

        public AccessPolicy System {
            get {
                if (_system == null) {
                    AccessPolicy policy = GetPolicyFromConfig("system");

                    if (!policy.Forbidden) {
                        policy.AccessKey = true;

                        if (string.IsNullOrEmpty(policy.Users)) {
                            policy.Users = "owners";
                        }
                    }

                    _system = policy;
                }

                return _system;
            }
        }


        private AccessPolicy GetPolicyFromConfig(string policyName) {
            var policy = new AccessPolicy();

            IConfigurationSection ps = _section?.GetSection(policyName) ?? null;
            if (ps == null) {
                return policy;
            }

            //
            // Users
            policy.Users = ps.GetValue("users", string.Empty).Trim();

            //
            // AccessKey
            policy.AccessKey = ps.GetValue("access_key", true);

            //
            // Forbidden
            policy.Forbidden = ps.GetValue("forbidden", false);

            //
            // ReadOnly
            policy.ReadOnly = ps.GetValue("read_only", false);

            return policy;
        }
    }
}
