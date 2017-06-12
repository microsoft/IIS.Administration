// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Web.Administration;

    class ActionElement : ConfigurationElement {
        
        public ActionType Type {
            get {
                return ((ActionType)(base["type"]));
            }
            set {
                base["type"] = ((int)(value));
            }
        }

        internal void CopyTo(ActionElement destination) {
            ConfigurationHelper.CopyAttributes(this, destination);
        }
    }
}

