// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering {
    using Web.Administration;

    public class DenyQueryStringSequenceElement : ConfigurationElement {

        public DenyQueryStringSequenceElement() {
        }

        public string Sequence {
            get {
                return ((string)(base["sequence"]));
            }
            set {
                base["sequence"] = value;
            }
        }
    }
}

