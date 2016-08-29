// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.IPRestrictions
{
    using Web.Administration;

    public sealed class ConcurrentRequestsElement : ConfigurationElement
    {

        public ConcurrentRequestsElement() { }

        private const string enabledAttribute = "enabled";
        private const string maxConcurrentRequestsAttribute = "maxConcurrentRequests";

        public bool Enabled
        {
            get
            {
                return (bool)base[enabledAttribute];
            }
            set
            {
                base[enabledAttribute] = value;
            }
        }

        public long MaxConcurrentRequests
        {
            get
            {
                return (long)base[maxConcurrentRequestsAttribute];
            }
            set
            {
                base[maxConcurrentRequestsAttribute] = value;
            }
        }
    }
}
