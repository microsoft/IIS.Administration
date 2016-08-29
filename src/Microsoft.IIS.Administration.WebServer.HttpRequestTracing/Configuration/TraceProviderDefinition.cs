// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRequestTracing
{
    using System;
    using Web.Administration;

    public sealed class TraceProviderDefinition : ConfigurationElement {

        private const string AreasAttribute = "areas";
        private const string NameAttribute = "name";
        private const string GuidAttribute = "guid";

        private TraceAreaDefinitionsCollection _areasCollection;

        public TraceAreaDefinitionsCollection Areas {
            get {
                if (_areasCollection == null) {
                    _areasCollection = (TraceAreaDefinitionsCollection)GetCollection(AreasAttribute, typeof(TraceAreaDefinitionsCollection));
                }

                return _areasCollection;
            }
        }

        public string Name {
            get {
                return (string)base[NameAttribute];
            }
            set {
                base[NameAttribute] = value;
            }
        }

        public Guid Guid
        {
            get
            {
                return Guid.Parse((string)base[GuidAttribute]);
            }
            set
            {
                // Bracket format {00000000-0000-0000-0000-000000000000}
                base[GuidAttribute] = value.ToString("B");
            }
        }
    }
}
