// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering
{
    using System;
    using Web.Administration;

    public class FilteringRuleCollection : ConfigurationElementCollectionBase<Rule> {
        
        public FilteringRuleCollection() {
        }
        
        public new Rule this[string name] {
            get {
                for (int i = 0; (i < this.Count); i = (i + 1)) {
                    Rule element = base[i];
                    if ((string.Equals(element.Name, name, StringComparison.OrdinalIgnoreCase) == true)) {
                        return element;
                    }
                }
                return null;
            }
        }
        
        protected override Rule CreateNewElement(string elementTagName) {
            return new Rule();
        }
        
        public Rule Add(string name) {
            Rule element = this.CreateElement();
            element.Name = name;
            return base.Add(element);
        }
    }
}
