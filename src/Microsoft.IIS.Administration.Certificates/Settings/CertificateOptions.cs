// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Certificates
{
    using Extensions.Configuration;
    using Files;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    class CertificateOptions : ICertificateOptions
    {
        private List<IStore> _stores = new List<IStore>();

        private CertificateOptions() { }

        public IEnumerable<IStore> Stores {
            get {
                return _stores;
            }
        }

        public static CertificateOptions FromConfiguration(IConfiguration configuration)
        {
            var options = new CertificateOptions();

            if (configuration.GetSection("certificates").GetChildren().Count() > 0) {
                foreach (var child in configuration.GetSection("certificates:stores").GetChildren()) {
                    var store = Store.FromSection(child);
                    if (store != null) {
                        options.AddStore(store);
                    }
                }
            }

            return options;
        }

        public void AddStore(IStore store)
        {
            try {
                if (!string.IsNullOrEmpty(store.Path)) {
                    var p = PathUtil.GetFullPath(store.Path);
                    store = new Store() {
                        Name = store.Name,
                        StoreLocation = store.StoreLocation,
                        Claims = store.Claims,
                        Path = p
                    };
                }

                if (string.IsNullOrEmpty(store.Name)) {
                    throw new ArgumentNullException("Name");
                }
            }
            catch (ArgumentNullException e) {
                Log.Error(e, $"Invalid name in certificate options. Name must not be empty.");
                throw;
            }
            catch (ArgumentException e) {
                Log.Error(e, $"Invalid path '{store.Path}' in certificate options.");
                throw;
            }

            //
            // Only add the store if it doesn't exist
            if (!_stores.Any(loc => loc.Name.Equals(store.Name, StringComparison.OrdinalIgnoreCase)) 
                    && (string.IsNullOrEmpty(store.Path) || !_stores.Any(loc => !string.IsNullOrEmpty(loc.Path)))) {
                _stores.Add(store);
            }
        }
    }
}
