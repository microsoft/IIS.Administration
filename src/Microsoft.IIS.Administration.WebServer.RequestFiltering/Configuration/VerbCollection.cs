// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering {
    using System;
    using Web.Administration;

    public class VerbCollection : ConfigurationElementCollectionBase<VerbElement> {
        
        public VerbCollection() {
        }

        public bool AllowUnlisted {
            get {
                return ((bool)(base["allowUnlisted"]));
            }
            set {
                base["allowUnlisted"] = value;
            }
        }

        public bool ApplyToWebDAV {
            get {
                return ((bool)(base["applyToWebDAV"]));
            }
            set {
                base["applyToWebDAV"] = value;
            }
        }

        public new VerbElement this[string verb] {
            get {
                for (int i = 0; (i < this.Count); i = (i + 1)) {
                    VerbElement element = base[i];
                    if ((string.Equals(element.Verb, verb, StringComparison.OrdinalIgnoreCase) == true)) {
                        return element;
                    }
                }
                return null;
            }
        }

        public VerbElement Add(string verb, bool allowed) {
            VerbElement element = this.CreateElement();
            element.Verb = verb;
            element.Allowed = allowed;
            return base.Add(element);
        }

        protected override VerbElement CreateNewElement(string elementTagName) {
            return new VerbElement();
        }

    }
}
