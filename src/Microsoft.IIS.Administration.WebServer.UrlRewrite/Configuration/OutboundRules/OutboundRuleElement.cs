// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    sealed class OutboundRule : RuleElement {
        private OutboundAction _action;
        private OutboundMatch _match;

        public new OutboundAction Action {
            get {
                if ((this._action == null)) {
                    this._action = ((OutboundAction)(base.GetChildElement("action", typeof(OutboundAction))));
                }
                return this._action;
            }
        }

        public new OutboundMatch Match {
            get {
                if ((this._match == null)) {
                    this._match = ((OutboundMatch)(base.GetChildElement("match", typeof(OutboundMatch))));
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

