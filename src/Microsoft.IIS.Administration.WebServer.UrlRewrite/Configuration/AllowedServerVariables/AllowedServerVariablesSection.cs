// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Web.Administration;

    sealed class AllowedServerVariablesSection : ConfigurationSection
    {
        private AllowedServerVariableCollection _allowedServerVariables;

        public AllowedServerVariableCollection AllowedServerVariables {
            get {
                if ((this._allowedServerVariables == null)) {
                    this._allowedServerVariables = ((AllowedServerVariableCollection)(base.GetCollection(typeof(AllowedServerVariableCollection))));
                }
                return this._allowedServerVariables;
            }
        }
    }
}

