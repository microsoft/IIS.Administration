// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Certificates
{
    using Core;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    public interface IWindowsCertificateStore { }

    public class WindowsCertificateStore : ICertificateStore, IWindowsCertificateStore
    {
        private string _name;
        private IEnumerable<string> _claims;

        public WindowsCertificateStore(string name, IEnumerable<string> claims)
        {
            _name = name;
            _claims = claims;
        }

        public string Name {
            get {
                return _name;
            }
        }

        public IEnumerable<string> Claims {
            get {
                return _claims;
            }
        }

        public static bool Exists(string name)
        {
            using (X509Store store = new X509Store(name, StoreLocation.LocalMachine)) {
                try {
                    store.Open(OpenFlags.OpenExistingOnly);
                }
                catch (CryptographicException e) {
                    if (e.HResult == HResults.NotFound) {
                        return false;
                    }
                    throw;
                }
            }

            return true;
        }

        public async Task<IEnumerable<ICertificate>> GetCertificates()
        {
            EnsureAccess(CertificateAccess.Read);

            var certs = new List<ICertificate>();

            return await Task.Run(() => {
                using (X509Store store = new X509Store(Name, StoreLocation.LocalMachine)) {
                    store.Open(OpenFlags.OpenExistingOnly);

                    foreach (X509Certificate2 cert in store.Certificates) {
                        certs.Add(new Certificate(cert, this, cert.Thumbprint));
                        cert.Dispose();
                    }

                    return certs;
                }
            });
        }

        public async Task<ICertificate> GetCertificate(string thumbprint)
        {
            EnsureAccess(CertificateAccess.Read);

            foreach (var cert in await GetCertificates()) {
                if (cert.Thumbprint.Equals(thumbprint)) {
                    return cert;
                }
            }

            return null;
        }
        
        public Stream GetContent(ICertificate certificate, bool persistKey, string password)
        {
            throw new NotImplementedException();
        }

        private bool IsAccessAllowed(CertificateAccess access)
        {
            return ((!access.HasFlag(CertificateAccess.Read) || _claims.Contains("Read", StringComparer.OrdinalIgnoreCase))
                        && (!access.HasFlag(CertificateAccess.Delete) || _claims.Contains("Delete", StringComparer.OrdinalIgnoreCase))
                        && (!access.HasFlag(CertificateAccess.Create) || _claims.Contains("Create", StringComparer.OrdinalIgnoreCase))
                        && (!access.HasFlag(CertificateAccess.Export) || _claims.Contains("Export", StringComparer.OrdinalIgnoreCase)));
        }

        private void EnsureAccess(CertificateAccess access)
        {
            if (!IsAccessAllowed(access)) {
                throw new ForbiddenArgumentException("certificate_store", null, Name);
            }
        }
    }
}
