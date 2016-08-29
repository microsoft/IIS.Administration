// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRequestTracing
{
    using Web.Administration;

    public sealed class TraceArea : ConfigurationElement
    {

        private const string AreasAttribute = "areas";
        private const string ProviderAttribute = "provider";
        private const string VerbosityAttribute = "verbosity";

        public string Areas {
            get {
                return (string)base[AreasAttribute];
            }
            set {
                base[AreasAttribute] = value;
            }
        }

        public string Provider {
            get {
                return (string)base[ProviderAttribute];
            }
            set {
                base[ProviderAttribute] = value;
            }
        }

        public FailedRequestTracingVerbosity Verbosity {
            get {
                return (FailedRequestTracingVerbosity)base[VerbosityAttribute];
            }
            set {
                base[VerbosityAttribute] = (int)value;
            }
        }
    }
}
