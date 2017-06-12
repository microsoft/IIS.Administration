// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Web.Administration;

    sealed class TagsElement : ConfigurationElement {
        
        private TagCollection _tags;
        
        public string Name {
            get {
                return ((string)(base["name"]));
            }
            set {
                base["name"] = value;
            }
        }
        
        public TagCollection Tags {
            get {
                if ((this._tags == null)) {
                    this._tags = ((TagCollection)(base.GetCollection(typeof(TagCollection))));
                }
                return this._tags;
            }
        }
    }
}

