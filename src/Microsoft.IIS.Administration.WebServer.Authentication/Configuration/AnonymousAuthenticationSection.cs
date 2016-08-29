// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Authentication
{
    using Microsoft.Web.Administration;

    public class AnonymousAuthenticationSection : ConfigurationSection
    {

        private const string EnabledAttribute = "enabled";
        private const string PasswordAttribute = "password";
        private const string UserNameAttribute = "userName";

        public const string SECTION_PATH = "system.webServer/security/authentication/anonymousAuthentication";

        public AnonymousAuthenticationSection()
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

        public string Password
        {
            get
            {
                return (string)base[PasswordAttribute];
            }
            set
            {
                base[PasswordAttribute] = value;
            }
        }

        public string UserName
        {
            get
            {
                return (string)base[UserNameAttribute];
            }
            set
            {
                base[UserNameAttribute] = value;
            }
        }
    }
}
