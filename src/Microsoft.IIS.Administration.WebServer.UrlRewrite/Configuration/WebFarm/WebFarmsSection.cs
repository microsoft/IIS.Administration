// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Web.Administration;

    sealed class WebFarmsSection : ConfigurationSection {

        private WebfarmCollection _webfarms;

        public WebfarmCollection Webfarms {
            get {
                if ((this._webfarms == null)) {
                    this._webfarms = ((WebfarmCollection)(base.GetCollection(typeof(WebfarmCollection))));
                }
                return this._webfarms;
            }
        }
    }
}

