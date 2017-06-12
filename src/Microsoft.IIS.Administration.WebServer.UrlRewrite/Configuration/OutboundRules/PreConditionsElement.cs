// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Web.Administration;

    sealed class PreConditionsElement : ConfigurationElement, IName {

        private ConditionCollection _preCondition;

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

        public ConditionCollection PreCondition {
            get {
                if ((this._preCondition == null)) {
                    this._preCondition = ((ConditionCollection)(base.GetCollection(typeof(ConditionCollection))));
                }
                return this._preCondition;
            }
        }
    }
}

