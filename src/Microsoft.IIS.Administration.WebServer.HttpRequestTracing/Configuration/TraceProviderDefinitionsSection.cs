// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRequestTracing
{
    using Web.Administration;

    public sealed class TraceProviderDefinitionsSection : ConfigurationSection {

        private TraceProviderDefinitionsCollection _traceProviderDefinitionsCollection;

        public TraceProviderDefinitionsCollection TraceProviderDefinitions {
            get {
                if (_traceProviderDefinitionsCollection == null) {
                    _traceProviderDefinitionsCollection = (TraceProviderDefinitionsCollection)GetCollection(typeof(TraceProviderDefinitionsCollection));
                }

                return _traceProviderDefinitionsCollection;
            }
        }
    }
}
