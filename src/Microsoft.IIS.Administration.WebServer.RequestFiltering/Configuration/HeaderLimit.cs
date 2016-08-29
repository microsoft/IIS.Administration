// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering
{
    using Web.Administration;

    public class HeaderLimit : ConfigurationElement {
        
        public HeaderLimit() {
        }
        
        public string Header {
            get {
                return ((string)(base["header"]));
            }
            set {
                base["header"] = value;
            }
        }
        
        public long SizeLimit {
            get {
                return ((long)(base["sizeLimit"]));
            }
            set {
                base["sizeLimit"] = value;
            }
        }
    }
}
