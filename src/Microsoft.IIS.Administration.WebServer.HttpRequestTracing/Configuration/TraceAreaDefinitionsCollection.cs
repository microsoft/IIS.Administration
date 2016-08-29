// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRequestTracing
{
    using System;
    using Web.Administration;

    public sealed class TraceAreaDefinitionsCollection : ConfigurationElementCollectionBase<TraceAreaDefinition> {

        public new TraceAreaDefinition this[string name] {
            get {
                for (int i = 0; i < Count; i++) {
                    TraceAreaDefinition element = base[i];

                    if (String.Equals(element.Name, name, StringComparison.OrdinalIgnoreCase)) {
                        return element;
                    }
                }

                return null;
            }
        }

        protected override TraceAreaDefinition CreateNewElement(string elementTagName) {
            return new TraceAreaDefinition();
        }
    }
}
