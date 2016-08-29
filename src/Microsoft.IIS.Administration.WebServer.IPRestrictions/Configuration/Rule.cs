// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.IPRestrictions
{
    using System;
    using System.Net;
    using Web.Administration;

    public sealed class Rule : ConfigurationElement {

        private const string AllowedAttribute = "allowed";
        private const string DomainNameAttribute = "domainName";
        private const string IpAddressAttribute = "ipAddress";
        private const string SubnetMaskAttribute = "subnetMask";

        public Rule() {
        }

        public bool Allowed {
            get {
                return (bool)base[AllowedAttribute];
            }
            set {
                base[AllowedAttribute] = value;
            }
        }

        public string DomainName {
            get {
                return (string)base[DomainNameAttribute];
            }
            set {
                base[DomainNameAttribute] = value;
            }
        }

        public IPAddress IpAddress {
            get {
                // Do this since there is no default value for IpAddress in config and it might return
                // empty string
                string addressString = (string)base[IpAddressAttribute];
                if (String.IsNullOrEmpty(addressString)) {
                    return IPAddress.None;
                }

                return IPAddress.Parse(addressString);
            }
            set {
                base[IpAddressAttribute] = value.ToString();
            }
        }

        public string SubnetMask {
            get {
                return (string)base[SubnetMaskAttribute];
            }
            set {
                base[SubnetMaskAttribute] = value.ToString();
            }
        }
    }
}
