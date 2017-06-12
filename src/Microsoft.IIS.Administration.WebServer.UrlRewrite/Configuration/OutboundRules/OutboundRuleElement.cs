// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    sealed class OutboundRuleElement : RuleElement {
        private OutboundActionElement _action;
        private OutboundMatchElement _match;

        public new OutboundActionElement Action {
            get {
                if ((this._action == null)) {
                    this._action = ((OutboundActionElement)(base.GetChildElement("action", typeof(OutboundActionElement))));
                }
                return this._action;
            }
        }

        public new OutboundMatchElement Match {
            get {
                if ((this._match == null)) {
                    this._match = ((OutboundMatchElement)(base.GetChildElement("match", typeof(OutboundMatchElement))));
                }
                return this._match;
            }
        }

        public string PreCondition {
            get {
                return ((string)(base["preCondition"]));
            }
            set {
                base["preCondition"] = value;
            }
        }
    }
}

