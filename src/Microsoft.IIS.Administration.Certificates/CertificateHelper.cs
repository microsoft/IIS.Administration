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

    public static class CertificateHelper
    {
        private static readonly Fields RefFields = new Fields("name", "id", "issued_by", "subject", "valid_to", "thumbprint");

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

        public static object ToJsonModelRef(X509Certificate2 cert, StoreName storeName, StoreLocation storeLocation, Fields fields = null)
        {
            if (fields == null || !fields.HasFields) {
                return ToJsonModel(cert, storeName, storeLocation, RefFields, false);
            }
            else {
                return ToJsonModel(cert, storeName, storeLocation, fields, false);
            }
        }

        internal static object ToJsonModel(X509Certificate2 cert, StoreName storeName, StoreLocation storeLocation, Fields fields = null, bool full = true)
        {
            if (cert == null) {
                return null;
            }

            if (fields == null) {
                fields = Fields.All;
            }

            dynamic obj = new ExpandoObject();

            //
            // name
            if (fields.Exists("name")) {
                obj.name = GetCertName(cert);
            }

            //
            // friendly_name
            if (fields.Exists("friendly_name")) {
                obj.friendly_name = cert.FriendlyName;
            }

            //
            // id
            obj.id = new CertificateId(cert.Thumbprint, storeName, storeLocation).Uuid;

            //
            // dns_name
            if (fields.Exists("dns_name")) {
                obj.dns_name = cert.GetNameInfo(X509NameType.DnsName, false);
            }

            //
            // simple_name
            if (fields.Exists("simple_name")) {
                obj.simple_name = cert.GetNameInfo(X509NameType.SimpleName, false);
            }

            //
            // issued_by
            if (fields.Exists("issued_by")) {
                obj.issued_by = cert.Issuer;
            }

            //
            // subject
            if (fields.Exists("subject")) {
                obj.subject = cert.Subject;
            }

            //
            // thumbprint
            if (fields.Exists("thumbprint")) {
                obj.thumbprint = cert.Thumbprint;
            }

            //
            // hash_algorithm
            if (fields.Exists("hash_algorithm")) {
                obj.hash_algorithm = cert.SignatureAlgorithm.FriendlyName;
            }

            //
            // valid_from
            if (fields.Exists("valid_from")) {
                obj.valid_from = cert.NotBefore.ToUniversalTime();
            }

            //
            // valid_to
            if (fields.Exists("valid_to")) {
                obj.valid_to = cert.NotAfter.ToUniversalTime();
            }

            //
            // version
            if (fields.Exists("version")) {
                obj.version = cert.Version.ToString();
            }

            //
            // intended_purposes
            if (fields.Exists("intended_purposes")) {
                obj.intended_purposes = GetEnhancedUsages(cert);
            }

            //
            // store
            if (fields.Exists("store")) {
                obj.store = new {
                    name = Enum.GetName(typeof(StoreName), storeName),
                    location = Enum.GetName(typeof(StoreLocation), storeLocation)
                };
            }

            return Core.Environment.Hal.Apply(Defines.Resource.Guid, obj, full);
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
