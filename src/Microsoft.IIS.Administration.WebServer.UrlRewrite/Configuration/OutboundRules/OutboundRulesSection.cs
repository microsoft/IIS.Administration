// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Web.Administration;

    sealed class OutboundRulesSection : ConfigurationSection {

        private TagsCollection _tags;
        
        private PreConditionsCollection _preConditions;
        
        private OutboundRulesCollection _rules;

        public PreConditionsCollection PreConditions {
            get {
                if ((this._preConditions == null)) {
                    this._preConditions = ((PreConditionsCollection)(base.GetCollection("preConditions", typeof(PreConditionsCollection))));
                }
                return this._preConditions;
            }
        }

        public bool RewriteBeforeCache {
            get {
                return ((bool)(base["rewriteBeforeCache"]));
            }
            set {
                base["rewriteBeforeCache"] = value;
            }
        }

        public OutboundRulesCollection Rules {
            get {
                if ((this._rules == null)) {
                    this._rules = ((OutboundRulesCollection)(base.GetCollection(typeof(OutboundRulesCollection))));
                }
                return this._rules;
            }
        }

        public TagsCollection Tags {
            get {
                if ((this._tags == null)) {
                    this._tags = ((TagsCollection)(base.GetCollection("customTags",typeof(TagsCollection))));
                }
                return this._tags;
            }
        }
    }
}

