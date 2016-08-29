// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering {
    using System;
    using Web.Administration;

    public class DenyUrlSequenceCollection : ConfigurationElementCollectionBase<DenyUrlSequence> {
        
        public DenyUrlSequenceCollection() {
        }
        
        public new DenyUrlSequence this[string sequence] {
            get {
                for (int i = 0; (i < this.Count); i = (i + 1)) {
                    DenyUrlSequence element = base[i];
                    if ((string.Equals(element.Sequence, sequence, StringComparison.OrdinalIgnoreCase) == true)) {
                        return element;
                    }
                }
                return null;
            }
        }

        public DenyUrlSequence Add(string sequence) {
            DenyUrlSequence element = this.CreateElement();
            element.Sequence = sequence;
            return base.Add(element);
        }

        protected override DenyUrlSequence CreateNewElement(string elementTagName) {
            return new DenyUrlSequence();
        }

    }
}
