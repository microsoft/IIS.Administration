// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using System;
    using Web.Administration;

    sealed class ConditionCollection : ConfigurationElementCollectionBase<ConditionElement> {
        
        public LogicalGrouping LogicalGrouping {
            get {
                return ((LogicalGrouping)(base["logicalGrouping"]));
            }
            set {
                base["logicalGrouping"] = ((int)(value));
            }
        }

        public bool TrackAllCaptures {
            get {
                return ((bool)(base["trackAllCaptures"]));
            }
            set {
                base["trackAllCaptures"] = value;
            }
        }

        public ConditionElement this[string input, string pattern, MatchType matchType, bool ignoreCase, bool negate] {
            get {
                for (int i = 0; (i < this.Count); i = (i + 1)) {
                    ConditionElement element = base[i];
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

        public ConditionElement Add(string input) {
            ConditionElement element = this.CreateElement();
            element.Input = input;
            return base.Add(element);
        }

        private ConditionElement AddCopy(ConditionElement condition) {
            ConditionElement element = CreateElement();

            CopyInfo(condition, element);

            return Add(element);
        }

        public ConditionElement AddCopyAt(int index, ConditionElement condition) {
            ConditionElement element = CreateElement();

            CopyInfo(condition, element);

            return AddAt(index, element);
        }

        private static void CopyInfo(ConditionElement source, ConditionElement destination) {
            ConfigurationHelper.CopyAttributes(source, destination);

            ConfigurationHelper.CopyMetadata(source, destination);
        }

        internal void CopyTo(ConditionCollection destination) {
            foreach (ConditionElement element in this) {
                destination.AddCopy(element);
            }
            destination.LogicalGrouping = LogicalGrouping;
            destination.TrackAllCaptures = TrackAllCaptures;
        }

        protected override ConditionElement CreateNewElement(string elementTagName) {
            return new ConditionElement();
        }

    }
}

