// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Certificates
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Cryptography;
    using Core.Utils;
    using System.Dynamic;
    using System.IO;

    public static class CertificateHelper
    {
        private static readonly Fields RefFields = new Fields("name", "id", "issued_by", "subject", "valid_to", "thumbprint");

        // Contains IDisposables
        public static IEnumerable<Cert> GetCertificates(string storeName, StoreLocation storeLocation)
        {
            var certs = new List<Cert>();

            using (X509Store store = GetStore(storeName, storeLocation, FileAccess.Read)) {
                if (store != null) {
                    foreach (X509Certificate2 cert in store.Certificates) {
                        certs.Add(new Cert(cert, storeName, storeLocation));
                    }
                }
            }

            return certs;
        }

        public static Cert GetCert(string thumbprint, string storeName, StoreLocation storeLocation)
        {
            Cert targetCert = null;
            using (X509Store store = GetStore(storeName, storeLocation, FileAccess.Read)) {
                if (store != null) {
                    foreach (X509Certificate2 cert in store.Certificates) {
                        if (cert.Thumbprint == thumbprint) {
                            targetCert = new Cert(cert, storeName, storeLocation);
                        }
                        else {
                            cert.Dispose();
                        }
                    }
                }
            }

            return targetCert;
        }

        public static object ToJsonModelRef(Cert cert, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return ToJsonModel(cert, RefFields, false);
            }
            else {
                return ToJsonModel(cert, fields, false);
            }
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

        public static X509Store GetStore(string storeName, StoreLocation storeLocation, FileAccess access)
        {
            var store = new X509Store(storeName, storeLocation);

            try {
                store.Open(OpenFlags.OpenExistingOnly | (access.HasFlag(FileAccess.Write) ? OpenFlags.ReadWrite : OpenFlags.ReadOnly));
            }
            catch (CryptographicException) {
                return null;
            }

            return store;
        }

        internal static object ToJsonModel(Cert cert, Fields fields = null, bool full = true)
        {
            if (cert == null) {
                return null;
            }

            if (fields == null) {
                fields = Fields.All;
            }

            dynamic obj = new ExpandoObject();
            var certificate = cert.Certificate;

            //
            // name
            if (fields.Exists("name")) {
                obj.name = GetCertName(certificate);
            }

            //
            // friendly_name
            if (fields.Exists("friendly_name")) {
                obj.friendly_name = certificate.FriendlyName;
            }

            //
            // id
            obj.id = new CertificateId(certificate.Thumbprint, cert.StoreName, cert.StoreLocation).Uuid;

            //
            // dns_name
            if (fields.Exists("dns_name")) {
                obj.dns_name = certificate.GetNameInfo(X509NameType.DnsName, false);
            }

            //
            // simple_name
            if (fields.Exists("simple_name")) {
                obj.simple_name = certificate.GetNameInfo(X509NameType.SimpleName, false);
            }

            //
            // issued_by
            if (fields.Exists("issued_by")) {
                obj.issued_by = certificate.Issuer;
            }

            //
            // subject
            if (fields.Exists("subject")) {
                obj.subject = certificate.Subject;
            }

            //
            // thumbprint
            if (fields.Exists("thumbprint")) {
                obj.thumbprint = certificate.Thumbprint;
            }

            //
            // hash_algorithm
            if (fields.Exists("hash_algorithm")) {
                obj.hash_algorithm = certificate.SignatureAlgorithm.FriendlyName;
            }

            //
            // valid_from
            if (fields.Exists("valid_from")) {
                obj.valid_from = certificate.NotBefore.ToUniversalTime();
            }

            //
            // valid_to
            if (fields.Exists("valid_to")) {
                obj.valid_to = certificate.NotAfter.ToUniversalTime();
            }

            //
            // version
            if (fields.Exists("version")) {
                obj.version = certificate.Version.ToString();
            }

            //
            // intended_purposes
            if (fields.Exists("intended_purposes")) {
                obj.intended_purposes = GetEnhancedUsages(certificate);
            }

            //
            // has_private_key
            if (fields.Exists("private_key")) {
                obj.private_key = KeyToJsonModel(cert.Certificate);
            }

            //
            // store
            if (fields.Exists("store")) {
                obj.store = ToJsonModel(cert.StoreName, cert.StoreLocation);
            }

            return Core.Environment.Hal.Apply(Defines.Resource.Guid, obj, full);
        }

        private static string GetCertName(X509Certificate2 cert)
        {
            if (!string.IsNullOrEmpty(cert.FriendlyName)) {
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

        private static object ToJsonModel(string storeName, StoreLocation storeLocation)
        {
            using (X509Store store = GetStore(storeName, storeLocation, FileAccess.Read)) {
                return new {
                    name = store.Name,
                    location = Enum.GetName(typeof(StoreLocation), store.Location)
                };
            }
        }

        private static object KeyToJsonModel(X509Certificate2 cert)
        {
            if (cert == null || !cert.HasPrivateKey) {
                return null;
            }

            return new {
                exportable = Interop.IsPrivateKeyExportable(cert)
            };
        }
    }
}
