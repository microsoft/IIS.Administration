// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Authorization
{
    using System;
    using Web.Administration;

    public sealed class RuleCollection : ConfigurationElementCollectionBase<Rule> {

        public RuleCollection() {
        }

        public Rule this[string users, string roles, string verbs] {
            get {
                for (int i = 0; i < Count; i++) {
                    Rule element = base[i];
                    if (String.Equals(element.Users, users, StringComparison.OrdinalIgnoreCase) &&
                        String.Equals(element.Roles, roles, StringComparison.OrdinalIgnoreCase) &&
                        String.Equals(element.Verbs, verbs, StringComparison.OrdinalIgnoreCase)) {
                        
                        return element;
                    }
                }

                return null;
            }
        }

        public Rule Add(RuleAccessType accessType, 
            string users, string roles, string verbs) {
            Rule element = CreateElement();

            element.AccessType = accessType;

            if (!String.IsNullOrEmpty(users)) {
                element.Users = users;
            }

            if (!String.IsNullOrEmpty(roles)) {
                element.Roles = roles;
            }

            if (!String.IsNullOrEmpty(verbs)) {
                element.Verbs = verbs;
            }

            return Add(element);
        }

        protected override Rule CreateNewElement(string elementTagName) {
            return new Rule();
        }
    }
}
