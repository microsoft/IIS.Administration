// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpResponseHeaders
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Microsoft.Web.Administration;

    public sealed class NameValueConfigurationElement : ConfigurationElement {

        private const string NameAttribute = "name";
        private const string ValueAttribute = "value";

        public NameValueConfigurationElement() {
        }

        public string Name {
            get {
                return (string)base[NameAttribute];
            }
            set {
                base[NameAttribute] = value;
            }
        }

        public string Value {
            get {
                return (string)base[ValueAttribute];
            }
            set {
                base[ValueAttribute] = value;
            }
        }
    }
}
