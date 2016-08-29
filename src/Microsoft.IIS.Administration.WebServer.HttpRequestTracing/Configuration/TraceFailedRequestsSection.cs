// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRequestTracing
{
    using Web.Administration;

    public sealed class TraceFailedRequestsSection : ConfigurationSection {

        private TraceRuleCollection _traceUrlCollection;

        public TraceRuleCollection TraceRules {
            get {
                if (_traceUrlCollection == null) {
                    _traceUrlCollection = (TraceRuleCollection)GetCollection(typeof(TraceRuleCollection));
                }

                return _traceUrlCollection;
            }
        }
    }
}
