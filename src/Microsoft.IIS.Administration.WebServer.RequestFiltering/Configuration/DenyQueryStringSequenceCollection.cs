// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering {
    using System;
    using Web.Administration;

    public class DenyQueryStringSequenceCollection : ConfigurationElementCollectionBase<DenyQueryStringSequenceElement> {
        
        public DenyQueryStringSequenceCollection() {
        }
        
        public new DenyQueryStringSequenceElement this[string sequence] {
            get {
                for (int i = 0; (i < this.Count); i = (i + 1)) {
                    DenyQueryStringSequenceElement element = base[i];
                    if ((string.Equals(element.Sequence, sequence, StringComparison.OrdinalIgnoreCase) == true)) {
                        return element;
                    }
                }
                return null;
            }
        }
        
        protected override DenyQueryStringSequenceElement CreateNewElement(string elementTagName) {
            return new DenyQueryStringSequenceElement();
        }
        
        public DenyQueryStringSequenceElement Add(string sequence) {
            DenyQueryStringSequenceElement element = this.CreateElement();
            element.Sequence = sequence;
            return base.Add(element);
        }
    }
}
