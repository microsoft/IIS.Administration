// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Modules
{
    using Microsoft.Web.Administration;

    internal sealed class GlobalModulesCollection : ConfigurationElementCollectionBase<GlobalModule> {

        public GlobalModulesCollection() {
        }

        public GlobalModule Add(string name, string image) {
            GlobalModule element = CreateElement();

            element.Name = name;
            element.Image = image;

            return Add(element);
        }

        public void AddCopyAt(int index, GlobalModule element) {
            GlobalModule newElement = CreateElement();

            newElement.Image = element.Image;
            newElement.Name = element.Name;
            newElement.PreCondition = element.PreCondition;

            AddAt(index, newElement);
        }

        protected override GlobalModule CreateNewElement(string elementTagName) {
            return new GlobalModule();
        }
    }
}
