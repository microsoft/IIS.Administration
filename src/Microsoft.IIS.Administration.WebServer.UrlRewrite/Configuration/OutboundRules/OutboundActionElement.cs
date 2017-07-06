// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    sealed class OutboundAction : ActionElement {

        public string RewriteValue {
            get {
                return ((string)(base["value"]));
            }
            set {
                base["value"] = value;
            }
        }

        public bool ReplaceServerVariable {
            get {
                return ((bool)(base["replace"]));
            }
            set {
                base["replace"] = value;
            }
        }
    }
}

