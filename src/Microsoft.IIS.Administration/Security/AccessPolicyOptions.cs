// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Security {
    using Microsoft.Extensions.Configuration;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security;


    class AccessPolicyOptions {
        public AccessPolicyOptions(IConfiguration config) {
            IConfigurationSection section = config.GetSection("security:access_policy");
            if (section == null) {
                return;
            }

            Api = GetPolicies(section, "api", "users:administrators+AccessKey");
            ApiKeys = GetPolicies(section, "api_keys", "users:administrators");
            System = GetPolicies(section, "system", "users:owners+AccessKey");


            //
            // Check minimum requirements
            if (Api.FirstOrDefault(r => r.Equals("AccessKey", StringComparison.OrdinalIgnoreCase)) == null) {
                throw new SecurityException("security.access_policy.api requires AccessKey");
            }

            if (System.FirstOrDefault(r => r.Equals("AccessKey", StringComparison.OrdinalIgnoreCase)) == null &&
                System.FirstOrDefault(r => r.Equals("Forbidden", StringComparison.OrdinalIgnoreCase)) == null) {
                throw new SecurityException("security.access_policy.system requires AccessKey");
            }

            if (ApiKeys.Count() == 0) {
                throw new SecurityException("security.access_policy.api_keys cannot be empty");
            }
        }

        public IEnumerable<string> Api { get; private set; }
        public IEnumerable<string> ApiKeys { get; private set; }
        public IEnumerable<string> System { get; private set; }


        private IEnumerable<string> GetPolicies(IConfigurationSection section, string policyName, string defaultValue) {
            string values = section.GetValue(policyName, defaultValue);

            var result = new List<string>();

            foreach (var v in values.Split('+')) {
                if (!string.IsNullOrWhiteSpace(v)) {
                    result.Add(v.Trim());
                }
            }

            return result;
        }
    }
}
