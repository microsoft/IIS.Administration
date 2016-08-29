// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRequestTracing
{
    using Web.Administration;

    public sealed class TraceAreaDefinition : ConfigurationElement {

        private const string NameAttribute = "name";
        private const string ValueAttribute = "value";

        public string Name {
            get {
                return (string)base[NameAttribute];
            }
            set {
                base[NameAttribute] = value;
            }
        }

        public long Value
        {
            get {
                return (long)base[ValueAttribute];
            }
            set {
                base[ValueAttribute] = value;
            }
        }
    }
}
