// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Modules
{
    using Microsoft.Web.Administration;

    public sealed class ModulesSection : ConfigurationSection
    {

        private const string RunAllManagedModulesForAllRequestsAttribute = "runAllManagedModulesForAllRequests";
        private const string RunManagedModulesForWebDavRequestsAttribute = "runManagedModulesForWebDavRequests";

        private ModuleCollection _collection;

        public ModulesSection() {
        }

        public bool RunAllManagedModulesForAllRequests
        {
            get
            {
                return (bool)base[RunAllManagedModulesForAllRequestsAttribute];
            }
            set
            {
                base[RunAllManagedModulesForAllRequestsAttribute] = (bool)value;
            }
        }

        public bool RunManagedModulesForWebDavRequests
        {
            get
            {
                return (bool)base[RunManagedModulesForWebDavRequestsAttribute];
            }
            set
            {
                base[RunManagedModulesForWebDavRequestsAttribute] = (bool)value;
            }
        }

        public ModuleCollection Modules {
            get {
                if (_collection == null) {
                    _collection = (ModuleCollection)GetCollection(typeof(ModuleCollection));
                }

                return _collection;
            }
        }
    }
}
