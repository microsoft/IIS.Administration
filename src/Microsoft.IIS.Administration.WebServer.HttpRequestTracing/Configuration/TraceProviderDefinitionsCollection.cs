// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRequestTracing
{
    using System;
    using Web.Administration;

    public sealed class TraceProviderDefinitionsCollection : ConfigurationElementCollectionBase<TraceProviderDefinition> {

        public new TraceProviderDefinition this[string name] {
            get {
                for (int i = 0; i < Count; i++) {
                    TraceProviderDefinition definition = base[i];

                    if (String.Equals(definition.Name, name, StringComparison.OrdinalIgnoreCase)) {
                        return definition;
                    }
                }

                return null;
            }
        }

        protected override TraceProviderDefinition CreateNewElement(string elementTagName) {
            return new TraceProviderDefinition();
        }
    }
}
