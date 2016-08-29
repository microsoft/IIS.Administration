// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Authentication
{
    using Web.Administration;
    using System.Collections.Generic;

    public class WindowsAuthenticationSection : ConfigurationSection
    {
        private const string EnabledAttribute = "enabled";

        public const string SECTION_NAME = "system.webServer/security/authentication/windowsAuthentication";

        public WindowsAuthenticationSection()
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

        public TokenChecking TokenCheckingAttribute
        {
            get
            {
                return (TokenChecking)base.GetChildElement("extendedProtection").GetAttributeValue("tokenChecking");
            }
            set
            {
                base.GetChildElement("extendedProtection").SetAttributeValue("tokenChecking", value);
            }
        }

        public bool UseKernelMode
        {
            get
            {
                return (bool)base.GetAttributeValue("useKernelMode");
            }
            set
            {
                base.SetAttributeValue("useKernelMode", value);
            }
        }
        
        List<string> providers = new List<string>();
        public List<string> Providers
        {
            get
            {
                ConfigurationElementCollection providersCollection = base.GetCollection("providers");
                providers.Clear();
                foreach (ConfigurationElement e in providersCollection) {
                    providers.Add(e.GetAttributeValue("value").ToString());
                }
                return providers;
            }
            set
            {
                ConfigurationElementCollection providersCollection = base.GetCollection("providers");
                providersCollection.Clear();
                for (int i = 0; i < value.Count; ++i) {
                    ConfigurationElement addElement = providersCollection.CreateElement("add");
                    addElement.SetAttributeValue("value", (string)value[i]);
                    providersCollection.Add(addElement);
                }
            }
        }
    }
}
