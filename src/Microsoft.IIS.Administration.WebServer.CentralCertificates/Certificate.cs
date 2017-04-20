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

        public string SignatureAlgorithm {
            get {
                EnsureInit();
                return _cert.SignatureAlgorithm;
            }
        }

        public ICertificateStore Store {
            get {
                EnsureInit();
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
                using (var fs = _fileProvider.GetFileStream(_file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                    var bytes = new byte[fs.Length];
                    fs.Read(bytes, 0, bytes.Length);
                    using (X509Certificate2 cert = !string.IsNullOrEmpty(_privateKeyPassword) ?
                                                      new X509Certificate2(bytes, _privateKeyPassword) :
                                                      new X509Certificate2(bytes)) {

                        cert.FriendlyName = _file.Name;
                        _cert = new Certificates.Certificate(cert, _store, _file.Name);
                    }
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
