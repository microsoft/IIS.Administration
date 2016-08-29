// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering {

    using System;
    using Web.Administration;

    public class HiddenSegmentCollection : ConfigurationElementCollectionBase<HiddenSegment> {
        
        public HiddenSegmentCollection() {
        }
        
        public new HiddenSegment this[string segment] {
            get {
                for (int i = 0; (i < this.Count); i = (i + 1)) {
                    HiddenSegment element = base[i];
                    if ((string.Equals(element.Segment, segment, StringComparison.OrdinalIgnoreCase) == true)) {
                        return element;
                    }
                }
                return null;
            }
        }

        public HiddenSegment Add(string segment) {
            HiddenSegment element = this.CreateElement();
            element.Segment = segment;
            return base.Add(element);
        }

        protected override HiddenSegment CreateNewElement(string elementTagName) {
            return new HiddenSegment();
        }

    }
}
