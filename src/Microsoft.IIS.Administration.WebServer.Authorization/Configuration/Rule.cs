// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Authorization
{
    using Web.Administration;

    public sealed class Rule : ConfigurationElement {

        private const string UsersAttribute = "users";
        private const string RolesAttribute = "roles";
        private const string VerbsAttribute = "verbs";
        private const string AccessTypeAttribute = "accessType";

        public Rule() {
        }

        public RuleAccessType AccessType {
            get {
                return (RuleAccessType)base[AccessTypeAttribute];
            }
            set {
                base[AccessTypeAttribute] = (int)value;
            }
        }

        public string Roles {
            get {
                return (string)base[RolesAttribute];
            }
            set {
                base[RolesAttribute] = value;
            }
        }

        public string Users {
            get {
                return (string)base[UsersAttribute];
            }
            set {
                base[UsersAttribute] = value;
            }
        }

        public string Verbs {
            get {
                return (string)base[VerbsAttribute];
            }
            set {
                base[VerbsAttribute] = value;
            }
        }
    }
}
