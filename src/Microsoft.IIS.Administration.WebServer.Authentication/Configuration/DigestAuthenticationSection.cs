// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Authentication
{
    using Web.Administration;

    public class DigestAuthenticationSection : ConfigurationSection
    {

        private const string EnabledAttribute = "enabled";
        private const string RealmAttribute = "realm";

        public const string SECTION_PATH = "system.webServer/security/authentication/digestAuthentication";

        public DigestAuthenticationSection()
        {
        }

        public bool Enabled
        {
            get
            {
                return (bool)base[EnabledAttribute];
            }
            set
            {
                base[EnabledAttribute] = value;
            }
        }

        public string Realm
        {
            get
            {
                return (string)base[RealmAttribute];
            }
            set
            {
                base[RealmAttribute] = value;
            }
        }

    }
}
