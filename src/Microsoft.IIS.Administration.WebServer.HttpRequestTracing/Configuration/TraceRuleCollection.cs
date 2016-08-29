// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRequestTracing
{
    using System;
    using Web.Administration;

    public sealed class TraceRuleCollection : ConfigurationElementCollectionBase<TraceRule>
    {
        public new TraceRule this[string path] {
            get {
                for (int i = 0; i < Count; i++) {
                    TraceRule element = base[i];

                    if (string.Equals(element.Path, path, StringComparison.OrdinalIgnoreCase)) {
                        return element;
                    }
                }

                return null;
            }
        }

        public TraceRule Add(string path) {
            TraceRule element = CreateElement();

            element.Path = path;

            return Add(element);
        }

        protected override TraceRule CreateNewElement(string elementTagName) {
            return new TraceRule();
        }

        public void Remove(string path) {
            Remove(this[path]);
        }
    }
}
