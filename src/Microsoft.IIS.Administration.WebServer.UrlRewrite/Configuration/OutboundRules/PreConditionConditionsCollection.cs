// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using System;
    using Web.Administration;

    sealed class PreConditionConditionsCollection : ConfigurationElementCollectionBase<PreConditionConditionElement> {

        public PreConditionConditionElement this[string input, string pattern, PreConditionMatchType matchType, bool ignoreCase, bool negate] {
            get {
                for (int i = 0; (i < this.Count); i = (i + 1)) {
                    PreConditionConditionElement element = base[i];
                    if (string.Equals(element.Input, input, StringComparison.OrdinalIgnoreCase) &&
                        element.Pattern == pattern &&
                        element.IgnoreCase == ignoreCase &&
                        element.Negate == negate &&
                        element.MatchType == matchType) {
                        return element;
                    }
                }
                return null;
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

        public LogicalGrouping LogicalGrouping {
            get {
                return ((LogicalGrouping)(base["logicalGrouping"]));
            }
            set {
                base["logicalGrouping"] = ((int)(value));
            }
        }

        public PreConditionConditionElement Add(string input) {
            PreConditionConditionElement element = this.CreateElement();
            element.Input = input;
            return base.Add(element);
        }

        private PreConditionConditionElement AddCopy(PreConditionConditionElement condition) {
            PreConditionConditionElement element = CreateElement();

            CopyInfo(condition, element);

            return Add(element);
        }

        public PreConditionConditionElement AddCopyAt(int index, PreConditionConditionElement condition) {
            PreConditionConditionElement element = CreateElement();

            CopyInfo(condition, element);

            return AddAt(index, element);
        }

        private static void CopyInfo(PreConditionConditionElement source, PreConditionConditionElement destination) {
            ConfigurationHelper.CopyAttributes(source, destination);

            ConfigurationHelper.CopyMetadata(source, destination);
        }

        internal void CopyTo(PreConditionConditionsCollection destination) {
            foreach (PreConditionConditionElement element in this) {
                destination.AddCopy(element);
            }
            destination.LogicalGrouping = LogicalGrouping;
        }

        protected override PreConditionConditionElement CreateNewElement(string elementTagName) {
            return new PreConditionConditionElement();
        }

    }
}

