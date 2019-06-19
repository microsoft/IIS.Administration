// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.CentralCertificates
{
    using System;
    using System.Collections.Generic;
    using Certificates;
    using Files;
    using System.Security.Cryptography.X509Certificates;
    using System.ComponentModel;
    using Core;
    using System.Security.Cryptography;
    using System.IO;

    public class Certificate : ICertificate
    {
        private IFileInfo _file;
        private ICertificate _cert;
        private ICertificateStore _store;
        private IFileProvider _fileProvider;
        private string _privateKeyPassword;

        public Certificate(IFileInfo file, ICertificateStore store, IFileProvider fileProvider, string privateKeyPassword)
        {
            _file = file;
            _store = store;
            _fileProvider = fileProvider;
            _privateKeyPassword = privateKeyPassword;
        }

        public string Alias {
            get {
                EnsureInit();
                return _cert.Alias;
            }
        }

        public DateTime Expires {
            get {
                EnsureInit();
                return _cert.Expires;
            }
        }

        public bool HasPrivateKey {
            get {
                EnsureInit();
                return _cert.HasPrivateKey;
            }
        }

        public string Id {
            get {
                EnsureInit();
                return _cert.Id;
            }
        }

        public bool IsPrivateKeyExportable {
            get {
                EnsureInit();
                return _cert.IsPrivateKeyExportable;
            }
        }

        public string Issuer {
            get {
                EnsureInit();
                return _cert.Issuer;
            }
        }

        public IEnumerable<string> Purposes {
            get {
                EnsureInit();
                return _cert.Purposes;
            }
        }

        public IEnumerable<string> PurposesOID
        {
            get
            {
                EnsureInit();
                return _cert.PurposesOID;
            }
        }

        public string SignatureAlgorithm {
            get {
                EnsureInit();
                return _cert.SignatureAlgorithm;
            }
        }

        public string SignatureAlgorithmOID
        {
            get
            {
                EnsureInit();
                return _cert.SignatureAlgorithmOID;
            }
        }

        public ICertificateStore Store {
            get {
                return _cert.Store;
            }
        }

        public string Subject {
            get {
                EnsureInit();
                return _cert.Subject;
            }
        }

        public IEnumerable<string> SubjectAlternativeNames {
            get {
                EnsureInit();
                return _cert.SubjectAlternativeNames;
            }
        }

        public string Thumbprint {
            get {
                EnsureInit();
                return _cert.Thumbprint;
            }
        }

        public DateTime ValidFrom {
            get {
                EnsureInit();
                return _cert.ValidFrom;
            }
        }

        public int Version {
            get {
                EnsureInit();
                return _cert.Version;
            }
        }

        private void EnsureInit()
        {
            if (_cert != null) {
                return;
            }

            try {
                using (X509Certificate2 cert = !string.IsNullOrEmpty(_privateKeyPassword) ?
                                                  new X509Certificate2(_file.Path, _privateKeyPassword) :
                                                  new X509Certificate2(_file.Path)) {

                    cert.FriendlyName = _file.Name;
                    _cert = new Certificates.Certificate(cert, _store, _file.Name);
                }
            }
            catch (UnauthorizedAccessException) {
                throw new ForbiddenArgumentException("certificate_store", "Cannot access store", _store.Name);
            }
            catch (CryptographicException e) {
                if (e.HResult == HResults.IncorrectPassword) {
                    throw new ForbiddenArgumentException("certificate_store", "Invalid private key password", _store.Name, e);
                }
                throw;
            }
        }
    }
}
