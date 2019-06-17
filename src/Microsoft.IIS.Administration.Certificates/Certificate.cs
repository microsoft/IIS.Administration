// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Certificates
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;

    public class Certificate : ICertificate
    {
        private const string OIDEnhancedKeyUsage = "2.5.29.37";
        private const string OIDSubjectAlternativeName = "2.5.29.17";

        private ICertificateStore _store;
        private string _id;
        private string _alias;
        private string _issuer;
        private DateTime _expires;
        private DateTime _validFrom;
        private Oid _signatureAlgorithm;
        private string _subject;
        private string _thumbprint;
        private int _version;
        private bool _hasPrivateKey;
        private bool _isPrivateKeyExportable;
        private List<string> _purposes = new List<string>();
        private List<string> _subjectAlternativeNames = new List<string>();

        public Certificate(X509Certificate2 certificate, ICertificateStore store, string id)
        {
            _store = store;
            _id = id;
            _alias = certificate.FriendlyName;
            _issuer = certificate.Issuer;
            _expires = certificate.NotAfter;
            _validFrom = certificate.NotBefore;
            _signatureAlgorithm = certificate.SignatureAlgorithm;
            _subject = certificate.Subject;
            _thumbprint = certificate.Thumbprint;
            _version = certificate.Version;
            _hasPrivateKey = certificate.HasPrivateKey;

            foreach (X509Extension extension in certificate.Extensions) {
                if (extension.Oid.Value == OIDEnhancedKeyUsage) {
                    X509EnhancedKeyUsageExtension ext = (X509EnhancedKeyUsageExtension)extension;
                    OidCollection oids = ext.EnhancedKeyUsages;
                    foreach (Oid oid in oids)
                    {
                        _purposes.Add(oid.Value);
                        _purposes.Add(oid.FriendlyName);
                    }
                }

                if (extension.Oid.Value == OIDSubjectAlternativeName) {
                    _subjectAlternativeNames.AddRange(extension.Format(true).Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
                }
            }

            if (_hasPrivateKey) {
                _isPrivateKeyExportable = Interop.IsPrivateKeyExportable(certificate);
            }
        }

        public string Id {
            get {
                return _id;
            }
        }

        public IEnumerable<string> Purposes {
            get {
                return _purposes.AsEnumerable();
            }
        }

        public IEnumerable<string> SubjectAlternativeNames {
            get {
                return _subjectAlternativeNames.AsEnumerable();
            }
        }

        public string Alias {
            get {
                return _alias;
            }
        }

        public bool HasPrivateKey {
            get {
                return _hasPrivateKey;
            }
        }

        public bool IsPrivateKeyExportable {
            get {
                return _isPrivateKeyExportable;
            }
        }

        public string Issuer {
            get {
                return _issuer;
            }
        }

        public DateTime Expires {
            get {
                return _expires;
            }
        }

        public DateTime ValidFrom {
            get {
                return _validFrom;
            }
        }

        public string SignatureAlgorithm {
            get {
                return _signatureAlgorithm.FriendlyName;
            }
        }

        public ICertificateStore Store {
            get {
                return _store;
            }
        }

        public string Subject {
            get {
                return _subject;
            }
        }

        public string Thumbprint {
            get {
                return _thumbprint;
            }
        }

        public int Version {
            get {
                return _version;
            }
        }
    }
}
