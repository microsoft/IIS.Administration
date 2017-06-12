// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Web.Administration;

    sealed class SetCollection : ConfigurationElementCollectionBase<SetElement> {

        public SetElement Add(string name) {
            SetElement element = this.CreateElement();
            element.Name = name;
            return base.Add(element);
        }

        private SetElement AddCopy(SetElement source) {
            SetElement element = CreateElement();

            CopyInfo(source, element);

            return Add(element);
        }

        private static void CopyInfo(SetElement source, SetElement destination) {
            ConfigurationHelper.CopyAttributes(source, destination);

            ConfigurationHelper.CopyMetadata(source, destination);
        }

        internal void CopyTo(SetCollection destination) {
            foreach (SetElement element in this) {
                destination.AddCopy(element);
            }
        }

        protected override SetElement CreateNewElement(string elementTagName) {
            return new SetElement();
        }

    }
}

