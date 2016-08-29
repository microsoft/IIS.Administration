// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering
{
    using Web.Administration;

    public class DenyUrlSequence : ConfigurationElement {
        
        public DenyUrlSequence() {
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
