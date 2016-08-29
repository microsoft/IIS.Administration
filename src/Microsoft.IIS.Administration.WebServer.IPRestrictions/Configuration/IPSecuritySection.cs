// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.IPRestrictions
{
    using Web.Administration;

    public sealed class IPSecuritySection : ConfigurationSection {

        private const string AllowUnlistedAttribute = "allowUnlisted";
        private const string denyActionAttribute = "denyAction";
        private const string EnableReverseDnsAttribute = "enableReverseDns";
        private const string EnableProxyModeAttribute = "enableProxyMode";

        private IPAddressFilterCollection _ipAddressCollection;

        public IPSecuritySection() {
        }

        public bool AllowUnlisted {
            get {
                return (bool)base[AllowUnlistedAttribute];
            }
            set {
                base[AllowUnlistedAttribute] = value;
            }
        }

        public DenyActionType DenyAction {
            get {
                return (DenyActionType)base[denyActionAttribute];
            }
            set {
                base[denyActionAttribute] = value;
            }
        }

        public bool EnableReverseDns {
            get { 
                return (bool)base[EnableReverseDnsAttribute];
            }
            set {
                base[EnableReverseDnsAttribute] = value;
            }
        }

        public bool EnableProxyMode {
            get {
                return (bool)base[EnableProxyModeAttribute];
            }
            set {
                base[EnableProxyModeAttribute] = value;
            }
        }

        public IPAddressFilterCollection IpAddressFilters {
            get {
                if (_ipAddressCollection == null) {
                    _ipAddressCollection = (IPAddressFilterCollection)GetCollection(typeof(IPAddressFilterCollection));
                }

                return _ipAddressCollection;
            }
        }
    }
}
