// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Certificates
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;

    public class Certificate : ICertificate
    {
        private const string OIDEnhancedKeyUsage = "2.5.29.37";
        private const string OIDSubjectAlternativeName = "2.5.29.17";

        private List<string> _purposes = new List<string>();
        private List<string> _purposesOID = new List<string>();
        private List<string> _subjectAlternativeNames = new List<string>();

        public string Id { get; }
        public string Alias { get; }
        public bool HasPrivateKey { get; }
        public bool IsPrivateKeyExportable { get; }
        public string Issuer { get; }
        public DateTime Expires { get; }
        public DateTime ValidFrom { get; }
        public string SignatureAlgorithm { get; }
        public string SignatureAlgorithmOID { get; }
        public ICertificateStore Store { get; }
        public string Subject { get; }
        public string Thumbprint { get; }
        public int Version { get; }

        public IEnumerable<string> Purposes
        {
            get
            {
                return _purposes;
            }
        }

        public IEnumerable<string> PurposesOID
        {
            get
            {
                return _purposesOID;
            }
        }

        public IEnumerable<string> SubjectAlternativeNames
        {
            get
            {
                return _subjectAlternativeNames;
            }
        }

        public Certificate(X509Certificate2 certificate, ICertificateStore store, string id)
        {
            Store = store;
            Id = id;
            Alias = certificate.FriendlyName;
            Issuer = certificate.Issuer;
            Expires = certificate.NotAfter;
            ValidFrom = certificate.NotBefore;
            SignatureAlgorithm = certificate.SignatureAlgorithm.FriendlyName;
            SignatureAlgorithmOID = certificate.SignatureAlgorithm.Value;
            Subject = certificate.Subject;
            Thumbprint = certificate.Thumbprint;
            Version = certificate.Version;
            HasPrivateKey = certificate.HasPrivateKey;

            foreach (X509Extension extension in certificate.Extensions) {
                if (extension.Oid.Value == OIDEnhancedKeyUsage) {
                    X509EnhancedKeyUsageExtension ext = (X509EnhancedKeyUsageExtension)extension;
                    OidCollection oids = ext.EnhancedKeyUsages;
                    foreach (Oid oid in oids)
                    {
                        _purposesOID.Add(oid.Value);
                        _purposes.Add(oid.FriendlyName);
                    }
                }

                if (extension.Oid.Value == OIDSubjectAlternativeName) {
                    _subjectAlternativeNames.AddRange(extension.Format(true).Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
                }
            }

            if (HasPrivateKey) {
                IsPrivateKeyExportable = Interop.IsPrivateKeyExportable(certificate);
            }
        }
    }
}
