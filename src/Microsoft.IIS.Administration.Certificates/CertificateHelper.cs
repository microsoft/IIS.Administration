// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Certificates
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Security.Cryptography.X509Certificates;
    using Core;
    using System.Security.Cryptography;
    public static class CertificateHelper
    {
        public const StoreName STORE_NAME = StoreName.My;
        public const StoreLocation STORE_LOCATION = StoreLocation.LocalMachine;

        // Contains IDisposables
        public static IEnumerable<X509Certificate2> GetCertificates(StoreName storeName, StoreLocation storeLocation)
        {
            List<X509Certificate2> certs = new List<X509Certificate2>();

            using (X509Store myStore = new X509Store(storeName, storeLocation)) {
                myStore.Open(OpenFlags.OpenExistingOnly);

                foreach (X509Certificate2 cert in myStore.Certificates) {

                    // Add certificates to a list which is easier to work with
                    certs.Add(cert);
                }
            }

            return certs;
        }

        public static X509Certificate2 GetCert(string thumbprint, StoreName storeName, StoreLocation storeLocation)
        {
            X509Certificate2 targetCert = null;
            using (X509Store store = new X509Store(storeName, storeLocation))
            {

                store.Open(OpenFlags.OpenExistingOnly);

                foreach (X509Certificate2 cert in store.Certificates)
                {
                    if (cert.Thumbprint == thumbprint)
                    {
                        targetCert = cert;
                    }
                    else
                    {
                        cert.Dispose();
                    }
                }
            }

            return targetCert;
        }

        public static object ToJsonModelRef(X509Certificate2 cert, StoreName storeName, StoreLocation storeLocation)
        {
            if (cert == null) {
                return null;
            }

            CertificateId id = new CertificateId(cert.Thumbprint, storeName, storeLocation);

            var obj = new {
                name = GetCertName(cert),
                id = id.Uuid,
                issued_by = cert.Issuer,
                valid_to = cert.NotAfter,
                thumbprint = cert.Thumbprint
            };

            return Core.Environment.Hal.Apply(Defines.Resource.Guid, obj, false);
        }

        public static object ToJsonModel(X509Certificate2 cert, StoreName storeName, StoreLocation storeLocation)
        {
            if (cert == null) {
                return null;
            }

            CertificateId id = new CertificateId(cert.Thumbprint, storeName, storeLocation);

            var obj = new {
                name = GetCertName(cert),
                friendly_name = cert.FriendlyName,
                id = id.Uuid,
                dns_name = cert.GetNameInfo(X509NameType.DnsName, false),
                simple_name = cert.GetNameInfo(X509NameType.SimpleName, false),
                issued_by = cert.Issuer,
                subject = cert.Subject,
                thumbprint = cert.Thumbprint,
                hash_algorithm = cert.SignatureAlgorithm.FriendlyName,
                valid_from = cert.NotBefore.ToUniversalTime(),
                valid_to = cert.NotAfter.ToUniversalTime(),
                version = cert.Version.ToString(),
                intended_purposes = GetEnhancedUsages(cert),

                store = new {
                    name = Enum.GetName(typeof(StoreName), storeName),
                    location = Enum.GetName(typeof(StoreLocation), storeLocation)
                }
            };

            return Core.Environment.Hal.Apply(Defines.Resource.Guid, obj);
        }

        public static IEnumerable<string> GetEnhancedUsages(X509Certificate2 cert)
        {
            List<string> usages = new List<string>();

            foreach (X509Extension extension in cert.Extensions) {
                if (extension.Oid.FriendlyName == "Enhanced Key Usage") {

                    X509EnhancedKeyUsageExtension ext = (X509EnhancedKeyUsageExtension)extension;
                    OidCollection oids = ext.EnhancedKeyUsages;
                    foreach (Oid oid in oids) {
                        usages.Add(oid.FriendlyName);
                    }
                }
            }

            return usages;
        }

        private static string GetCertName(X509Certificate2 cert)
        {
            if(!string.IsNullOrEmpty(cert.FriendlyName)) {
                return cert.FriendlyName;
            }

            string dnsName = cert.GetNameInfo(X509NameType.DnsName, false);
            if (!string.IsNullOrEmpty(dnsName)) {
                return dnsName;
            }

            string simpleName = cert.GetNameInfo(X509NameType.SimpleName, false);
            if (!string.IsNullOrEmpty(simpleName)) {
                return simpleName;
            }
            return cert.Thumbprint;
        }
    }
}
