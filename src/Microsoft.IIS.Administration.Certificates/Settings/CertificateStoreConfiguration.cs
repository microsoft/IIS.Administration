// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Certificates
{
    using Extensions.Configuration;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;

    class CertificateStoreConfiguration : ICertificateStoreConfiguration
    {
        public string Name { get; set; }
        public StoreLocation StoreLocation { get; set; }
        public string Path { get; set; }
        public IEnumerable<string> Claims { get; set; } = new List<string>();

        public static CertificateStoreConfiguration FromSection(IConfigurationSection section)
        {
            string name = section.GetValue("name", string.Empty);
            string path = section.GetValue<string>("path");
            List<string> claims = new List<string>();
            ConfigurationBinder.Bind(section.GetSection("claims"), claims);

            CertificateStoreConfiguration store = null;

            if (!string.IsNullOrEmpty(name)) {
                store = new CertificateStoreConfiguration() {
                    Name = name,
                    Path = path,
                    Claims = claims,
                    StoreLocation = StoreLocation.LocalMachine
                };
            }

            return store;
        }
    }
}
