// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Modules
{
    using System;
    using Microsoft.Web.Administration;

    public sealed class ModuleCollection : ConfigurationElementCollectionBase<Module> {

        public ModuleCollection() {
        }

        public new Module this[string name] {
            get {
                for (int i = 0; i < Count; i++) {
                    Module element = base[i];

                    if (String.Equals(element.Name, name, StringComparison.OrdinalIgnoreCase)) {
                        return element;
                    }
                }

                return null;
            }
        }

        public Module Add(string name) {
            Module element = CreateElement();

            element.Name = name;

            return Add(element);
        }

        public Module Add(string name, string type) {
            Module action = Add(name);

            action.Type = type;

            return action;
        }

        public Module AddCopy(Module moduleAction) {
            Module element = CreateElement();

            CopyInfo(moduleAction, element);

            return Add(element);
        }

        public Module AddCopyAt(int index, Module moduleAction) {
            Module element = CreateElement();

            CopyInfo(moduleAction, element);

            return AddAt(index, element);
        }

        private static void CopyInfo(Module source, Module destination) {
            destination.Name = source.Name;
            destination.Type = source.Type;
            destination.PreCondition = source.PreCondition;

            object o = source.GetMetadata("lockItem");
            if (o != null) {
                destination.SetMetadata("lockItem", o);
            }

            o = source.GetMetadata("lockAttributes");
            if (o != null) {
                destination.SetMetadata("lockAttributes", o);
            }

            o = source.GetMetadata("lockElements");
            if (o != null) {
                destination.SetMetadata("lockElements", o);
            }

            o = source.GetMetadata("lockAllAttributesExcept");
            if (o != null) {
                destination.SetMetadata("lockAllAttributesExcept", o);
            }

            o = source.GetMetadata("lockAllElementsExcept");
            if (o != null) {
                destination.SetMetadata("lockAllElementsExcept", o);
            }
        }

        protected override Module CreateNewElement(string elementTagName) {
            return new Module();
        }

        public void Remove(string name) {
            base.Remove(this[name]);
        }
    }
}
