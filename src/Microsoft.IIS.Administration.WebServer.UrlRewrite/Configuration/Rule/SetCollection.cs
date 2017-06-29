// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Web.Administration;

    sealed class SetCollection : ConfigurationElementCollectionBase<ServerVariableAssignment> {

        public ServerVariableAssignment Add(string name) {
            ServerVariableAssignment element = this.CreateElement();
            element.Name = name;
            return base.Add(element);
        }

        private ServerVariableAssignment AddCopy(ServerVariableAssignment source) {
            ServerVariableAssignment element = CreateElement();

            CopyInfo(source, element);

            return Add(element);
        }

        private static void CopyInfo(ServerVariableAssignment source, ServerVariableAssignment destination) {
            ConfigurationHelper.CopyAttributes(source, destination);

            ConfigurationHelper.CopyMetadata(source, destination);
        }

        internal void CopyTo(SetCollection destination) {
            foreach (ServerVariableAssignment element in this) {
                destination.AddCopy(element);
            }
        }

        protected override ServerVariableAssignment CreateNewElement(string elementTagName) {
            return new ServerVariableAssignment();
        }

    }
}

