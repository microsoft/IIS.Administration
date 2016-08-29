// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRequestTracing
{
    using Web.Administration;

    public sealed class TraceRule : ConfigurationElement {

        private const string FailureDefinitionsAttribute = "failureDefinitions";
        private const string PathAttribute = "path";
        private const string TraceAreasAttribute = "traceAreas";
        private const string CustomActionExeAttribute = "customActionExe";
        private const string CustomActionParamsAttribute = "customActionParams";
        private const string CustomActionTriggerLimitAttribute = "customActionTriggerLimit";

        private FailureDefinitions _failureDefinitions;
        private TraceAreaCollection _traceAreaCollection;

        public string CustomActionExe {
            get { 
                return (string)base[CustomActionExeAttribute];
            }
            set {
                base[CustomActionExeAttribute] = value;
            }
        }

        public string CustomActionParams {
            get {
                return (string)base[CustomActionParamsAttribute];
            }
            set {
                base[CustomActionParamsAttribute] = value;
            }
        }

        public long CustomActionTriggerLimit {
            get { 
                return (long)base[CustomActionTriggerLimitAttribute];
            }
            set {
                base[CustomActionTriggerLimitAttribute] = value;
            }
        }

        public FailureDefinitions FailureDefinition {
            get {
                if (_failureDefinitions == null) {
                    _failureDefinitions = (FailureDefinitions)GetChildElement(FailureDefinitionsAttribute, typeof(FailureDefinitions));
                }

                return _failureDefinitions;
            }
        }

        public string Path {
            get {
                return (string)base[PathAttribute];
            }
            set {
                base[PathAttribute] = value;
            }
        }

        public TraceAreaCollection TraceAreas {
            get {
                if (_traceAreaCollection == null) {
                    _traceAreaCollection = (TraceAreaCollection)GetCollection(TraceAreasAttribute, typeof(TraceAreaCollection));
                }

                return _traceAreaCollection;
            }
        }

    }
}
