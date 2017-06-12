// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Web.Administration;

    interface IName {
        string Name { get; set; }
    }

    class RuleElement : ConfigurationElement, IName {

        private ActionElement _action;
        private ConditionCollection _conditions;
        private MatchElement _match;

        public ActionElement Action {
            get {
                if ((this._action == null)) {
                    this._action = ((ActionElement)(base.GetChildElement("action", typeof(ActionElement))));
                }
                return this._action;
            }
        }

        public ConditionCollection Conditions {
            get {
                if ((this._conditions == null)) {
                    this._conditions = ((ConditionCollection)(base.GetCollection("conditions", typeof(ConditionCollection))));
                }
                return this._conditions;
            }
        }

        public bool Enabled {
            get {
                return ((bool)(base["enabled"]));
            }
            set {
                base["enabled"] = value;
            }
        }

        public MatchElement Match {
            get {
                if ((this._match == null)) {
                    this._match = ((MatchElement)(base.GetChildElement("match", typeof(MatchElement))));
                }
                return this._match;
            }
        }

        public string Name {
            get {
                return ((string)(base["name"]));
            }
            set {
                base["name"] = value;
            }
        }

        public PatternSyntax PatternSyntax {
            get {
                return ((PatternSyntax)(base["patternSyntax"]));
            }
            set {
                base["patternSyntax"] = ((int)(value));
            }
        }

        public bool StopProcessing {
            get {
                return ((bool)(base["stopProcessing"]));
            }
            set {
                base["stopProcessing"] = value;
            }
        }
    }
}

