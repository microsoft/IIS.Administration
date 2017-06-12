// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Web.Administration;

    sealed class ConditionElement : ConfigurationElement {

        public bool IgnoreCase {
            get {
                return ((bool)(base["ignoreCase"]));
            }
            set {
                base["ignoreCase"] = value;
            }
        }

        public string Input {
            get {
                return ((string)(base["input"]));
            }
            set {
                base["input"] = value;
            }
        }

        public MatchType MatchType {
            get {
                return ((MatchType)(base["matchType"]));
            }
            set {
                base["matchType"] = ((int)(value));
            }
        }

        public bool Negate {
            get {
                return ((bool)(base["negate"]));
            }
            set {
                base["negate"] = value;
            }
        }

        public string Pattern {
            get {
                return ((string)(base["pattern"]));
            }
            set {
                base["pattern"] = value;
            }
        }
    }
}

