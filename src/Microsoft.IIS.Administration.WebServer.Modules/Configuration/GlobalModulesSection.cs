// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Modules
{
    using Microsoft.Web.Administration;


    internal sealed class GlobalModulesSection : ConfigurationSection {

        private GlobalModulesCollection _collection;

        public GlobalModulesSection() {
        }

        public GlobalModulesCollection GlobalModules {
            get {
                if (_collection == null) {
                    _collection = (GlobalModulesCollection)GetCollection(typeof(GlobalModulesCollection));
                }

                return _collection;
            }
        }
    }
}
