// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.IPRestrictions
{
    using Web.Administration;

    public sealed class RequestsOvertimeElement : ConfigurationElement{

        private const string enabledAttribute = "enabled";
        private const string maxRequestsAttribute = "maxRequests";
        private const string timePeriodAttribute = "requestIntervalInMilliseconds";

        public RequestsOvertimeElement() { }

        public bool Enabled {
            get {
                return (bool)base[enabledAttribute];
            }
            set {
                base[enabledAttribute] = value;
            }
        }

        public long MaxRequests {
            get {
                return (long)base[maxRequestsAttribute];
            }
            set {
                base[maxRequestsAttribute] = value;
            }
        }

        // Milliseconds
        public long TimePeriod {
            get {
                return (long) base[timePeriodAttribute];
            }
            set {
                base[timePeriodAttribute] = value;
            }
        }
    }
}
