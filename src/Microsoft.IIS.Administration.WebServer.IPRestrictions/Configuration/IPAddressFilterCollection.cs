// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.IPRestrictions
{
    using System;
    using System.Net;
    using Web.Administration;

    public sealed class IPAddressFilterCollection : ConfigurationElementCollectionBase<Rule>
    {

        public IPAddressFilterCollection()
        {
        }

        public Rule Add(string domainName, IPAddress ipAddress, string subnetMask)
        {
            Rule element = CreateElement();

            if (!String.IsNullOrEmpty(domainName)) {
                element.DomainName = domainName;
            }

            if (ipAddress != IPAddress.None) {
                element.IpAddress = ipAddress;
            }
            if (!String.Equals(subnetMask, IPAddress.None.ToString(), StringComparison.Ordinal)) {
                element.SubnetMask = subnetMask;
            }

            return Add(element);
        }

        public Rule AddAt(int index, string domainName, IPAddress address, string subnetMask, bool allow)
        {
            Rule element = CreateElement();

            if (!String.IsNullOrEmpty(domainName)) {
                element.DomainName = domainName;
            }

            if (address != IPAddress.None) {
                element.IpAddress = address;
            }

            if (!String.Equals(subnetMask, IPAddress.None.ToString(), StringComparison.Ordinal)) {
                element.SubnetMask = subnetMask;
            }

            element.Allowed = allow;

            return AddAt(index, element);
        }

        public void AddCopy(Rule element)
        {
            Rule newElement = Add(element.DomainName, element.IpAddress, element.SubnetMask);
            newElement.Allowed = element.Allowed;

            CopyMetadata(element, newElement);
        }

        public void AddCopyAt(int index, Rule element)
        {
            Rule newElement = AddAt(index, element.DomainName,
                element.IpAddress, element.SubnetMask, element.Allowed);

            CopyMetadata(element, newElement);
        }

        public static void CopyMetadata(Rule oldElement, Rule newElement)
        {
            object o = oldElement.GetMetadata("lockItem");
            if (o != null) {
                newElement.SetMetadata("lockItem", o);
            }

            o = oldElement.GetMetadata("lockAttributes");
            if (o != null) {
                newElement.SetMetadata("lockAttributes", o);
            }

            o = oldElement.GetMetadata("lockElements");
            if (o != null) {
                newElement.SetMetadata("lockElements", o);
            }

            o = oldElement.GetMetadata("lockAllAttributesExcept");
            if (o != null) {
                newElement.SetMetadata("lockAllAttributesExcept", o);
            }

            o = oldElement.GetMetadata("lockAllElementsExcept");
            if (o != null) {
                newElement.SetMetadata("lockAllElementsExcept", o);
            }
        }

        internal Rule Find(string domainName, IPAddress address, string subnetMask)
        {
            for (int i = 0; i < Count; i++) {
                Rule element = this[i];
                if ((domainName != null) && // If I should check by domainName
                    !String.Equals(domainName, element.DomainName, StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                if (!IPAddress.Equals(address, element.IpAddress)) {
                    continue;
                }

                if (!String.Equals(subnetMask, element.SubnetMask, StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }

                return element;
            }

            return null;
        }

        protected override Rule CreateNewElement(string elementTagName) {
            return new Rule();
        }
    }
}
