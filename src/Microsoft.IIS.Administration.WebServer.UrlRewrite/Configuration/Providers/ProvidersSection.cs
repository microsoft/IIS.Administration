// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Web.Administration;

    internal sealed class ProvidersSection : ConfigurationSection {

        private ProvidersCollection _providers;

        public ProvidersCollection Providers {
            get {
                if ((this._providers == null)) {
                    this._providers = ((ProvidersCollection)(base.GetCollection(typeof(ProvidersCollection))));
                }
                return this._providers;
            }
        }

    }
}

