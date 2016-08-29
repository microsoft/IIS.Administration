// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Authorization
{
    using Web.Administration;

    public class AuthorizationSection : ConfigurationSection
    {

        private const string BypassLoginPagesAttribute = "bypassLoginPages";

        private RuleCollection _rules;

        public const string SECTION_PATH = "system.webServer/security/authorization";

        public AuthorizationSection()
        {
        }

        public bool BypassLoginPages
        {
            get
            {
                return (bool)base[BypassLoginPagesAttribute];
            }
            set
            {
                base[BypassLoginPagesAttribute] = value;
            }
        }

        public RuleCollection Rules
        {
            get
            {
                if (_rules == null) {
                    _rules = (RuleCollection)GetCollection(typeof(RuleCollection));
                }

                return _rules;
            }
        }
    }
}
