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
        private List<ICertificateStoreConfiguration> _stores = new List<ICertificateStoreConfiguration>();

        private CertificateOptions() { }

        public IEnumerable<ICertificateStoreConfiguration> Stores {
            get {
                return _stores;
            }
        }

        public static CertificateOptions FromConfiguration(IConfiguration configuration)
        {
            var options = new CertificateOptions();

            if (configuration.GetSection("certificates").GetChildren().Count() > 0) {
                foreach (var child in configuration.GetSection("certificates:stores").GetChildren()) {
                    var store = CertificateStoreConfiguration.FromSection(child);
                    if (store != null) {
                        options.AddStore(store);
                    }
                }
            }

            return options;
        }

        public void AddStore(ICertificateStoreConfiguration store)
        {
            try {
                if (!string.IsNullOrEmpty(store.Path)) {
                    var p = PathUtil.GetFullPath(store.Path);
                    store = new CertificateStoreConfiguration() {
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
                Log.Error(e, $"Invalid store name in certificate options. Name must not be empty.");
                throw;
            }
            catch (ArgumentException e) {
                Log.Error(e, $"Invalid store path '{store.Path}' in certificate options.");
                throw;
            }

            //
            // Replace the store if it exists
            int existing = _stores.FindIndex(s => s.Name.Equals(store.Name, StringComparison.OrdinalIgnoreCase));
            
            if (existing != -1) {
                _stores[existing] = store;
            }
            else {
                _stores.Add(store);
            }
        }
    }
}
