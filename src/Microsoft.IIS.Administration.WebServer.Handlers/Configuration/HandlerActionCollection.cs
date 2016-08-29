// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Handlers
{
    using System;
    using Microsoft.Web.Administration;

    public sealed class HandlerActionCollection : ConfigurationElementCollectionBase<Mapping> {

        public HandlerActionCollection() {
        }

        public new Mapping this[string name] {
            get {
                for (int i = 0; i < Count; i++) {
                    Mapping element = base[i];

                    if (String.Equals(element.Name, name, StringComparison.OrdinalIgnoreCase)) {
                        return element;
                    }
                }

                return null;
            }
        }

        public Mapping AddAt(int index, string name, string path, string verb) {
            Mapping element = CreateElement();

            element.Name = name;
            element.Path = path;
            element.Verb = verb;

            return AddAt(index, element);
        }

        public Mapping AddCopy(Mapping action) {
            Mapping element = CreateElement();

            CopyInfo(action, element);

            return Add(element);
        }

        public Mapping AddCopyAt(int index, Mapping action) {
            Mapping element = CreateElement();

            CopyInfo(action, element);

            return AddAt(index, element);
        }

        private static void CopyInfo(Mapping source, Mapping destination) {
            destination.Name = source.Name;
            destination.Modules = source.Modules;
            destination.Path = source.Path;
            destination.PreCondition = source.PreCondition;
            destination.RequireAccess = source.RequireAccess;
            destination.ResourceType = source.ResourceType;
            destination.ScriptProcessor = source.ScriptProcessor;
            destination.Type = source.Type;
            destination.Verb = source.Verb;
            destination.AllowPathInfo = source.AllowPathInfo;
            destination.ResponseBufferLimit = source.ResponseBufferLimit;

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

        protected override Mapping CreateNewElement(string elementTagName) {
            return new Mapping();
        }
    }
}
