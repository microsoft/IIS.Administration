// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Web.Administration;

    sealed class PreCondition : ConfigurationElement, IName {

        private PreConditionConditionsCollection _preCondition;

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

        public LogicalGrouping LogicalGrouping {
            get {
                return ((LogicalGrouping)(base["logicalGrouping"]));
            }
            set {
                base["logicalGrouping"] = ((int)(value));
            }
        }

        public PreConditionConditionsCollection Conditions {
            get {
                if ((this._preCondition == null)) {
                    this._preCondition = ((PreConditionConditionsCollection)(base.GetCollection(typeof(PreConditionConditionsCollection))));
                }
                return this._preCondition;
            }
        }
    }
}

