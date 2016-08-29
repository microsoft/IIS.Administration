// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.IPRestrictions
{
    using Web.Administration;

    public sealed class DynamicIPSecuritySection : ConfigurationSection
    {

        private const string proxyModeAttribute = "enableProxyMode";
        private const string loggingOnlyModeAttribute = "enableLoggingOnlyMode";
        private const string denyActionAttribute = "denyAction";
        private const string denyByRequestRateElement = "denyByRequestRate";
        private const string denyByConcurrentRequestsElement = "denyByConcurrentRequests";

        private ConcurrentRequestsElement _concurrentElement;
        private RequestsOvertimeElement _requestsRateElement;

        public DenyActionType DenyAction
        {
            get
            {
                return (DenyActionType)base[denyActionAttribute];
            }
            set
            {
                base[denyActionAttribute] = value;
            }
        }

        public bool ProxyMode
        {
            get
            {
                return (bool)base[proxyModeAttribute];
            }
            set
            {
                base[proxyModeAttribute] = value;
            }
        }

        public bool LoggingOnlyMode
        {
            get
            {
                return (bool)base[loggingOnlyModeAttribute];
            }
            set
            {
                base[loggingOnlyModeAttribute] = value;
            }
        }

        public ConcurrentRequestsElement DenyByConcurrentRequests
        {
            get
            {
                if (_concurrentElement == null) {
                    _concurrentElement = (ConcurrentRequestsElement)GetChildElement(denyByConcurrentRequestsElement, typeof(ConcurrentRequestsElement));
                }

                return _concurrentElement;
            }
        }

        public RequestsOvertimeElement DenyByRequestRate
        {
            get
            {
                if (_requestsRateElement == null) {
                    _requestsRateElement = (RequestsOvertimeElement)GetChildElement(denyByRequestRateElement, typeof(RequestsOvertimeElement));
                }

                return _requestsRateElement;
            }
        }
    }
}
