// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.CentralCertificates
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Certificates;
    using Core;
    using System.Linq;
    using System.ComponentModel;
    using System.IO;
    using System.Security.Cryptography.X509Certificates;
    using Win32;

    public interface ICentralCertificateStore { }

    class CentralCertificateStore : ICertificateStore
    {
        private const string REGKEY_CENTRAL_CERTIFICATE_STORE_PROVIDER = "SOFTWARE\\Microsoft\\IIS\\CentralCertProvider";
        private const string REGVAL_CERT_STORE_LOCATION = "CertStoreLocation";
        private const string REGVAL_USERNAME = "UserName";
        private const string REGVAL_PASSWORD = "Password";
        private const string REGVAL_PRIVATE_KEY_PASSWORD = "PrivateKeyPassword";
        private const string REGVAL_POLLING_INTERVAL = "PollingInterval";
        private const string REGVAL_ENABLED = "Enabled";
        private const int DEFAULT_POLLING_INTERVAL = 300; // seconds
        private readonly IEnumerable<string> _claims;
        private DateTime _lastRefresh;
        private bool _enabled;
        private int _pollingInterval;
        private string _physicalPath;
        private string _username;
        private string _encryptedPassword;
        private string _encryptedPrivateKeyPassword;

        public CentralCertificateStore(string name, IEnumerable<string> claims)
        {
            Name = name;
            _claims = claims;
            Refresh();
        }

        public bool Enabled {
            get {
                EnsureRefreshed();
                return _enabled;
            }
            set {

                using (RegistryKey key = GetRegKey()) {

                    if (!value) {
                        key.DeleteValue(REGVAL_USERNAME, false);
                        key.DeleteValue(REGVAL_PASSWORD, false);
                        key.DeleteValue(REGVAL_PRIVATE_KEY_PASSWORD, false);
                    }

                    key.SetValue(REGVAL_ENABLED, value ? 1 : 0);
                }

                _enabled = value;
            }
        }

        public int PollingInterval {
            get {
                EnsureRefreshed();
                return _pollingInterval;
            }
            set {

                using (RegistryKey key = GetRegKey()) {
                    key.SetValue(REGVAL_POLLING_INTERVAL, value);
                }

                _pollingInterval = value;
            }
        }

        public string PhysicalPath {
            get {
                EnsureRefreshed();
                return _physicalPath;
            }
            set {
                using (RegistryKey key = GetRegKey()) {
                    key.SetValue(REGVAL_CERT_STORE_LOCATION, value, RegistryValueKind.String);
                }

                _physicalPath = value;
            }
        }

        public string UserName {
            get {
                EnsureRefreshed();
                return _username;
            }
            set {
                using (RegistryKey key = GetRegKey()) {
                    key.SetValue(REGVAL_USERNAME, value, RegistryValueKind.String);
                }

                _username = value;
            }
        }

        public string EncryptedPassword {
            get {
                EnsureRefreshed();
                return _encryptedPassword;
            }
            set {
                using (RegistryKey key = GetRegKey()) {
                    key.SetValue(REGVAL_PASSWORD, value, RegistryValueKind.String);
                }

                _encryptedPassword = value;
            }
        }

        public string EncryptedPrivateKeyPassword {
            get {
                EnsureRefreshed();
                return _encryptedPrivateKeyPassword;
            }
            set {
                using (RegistryKey key = GetRegKey()) {
                    key.SetValue(REGVAL_PRIVATE_KEY_PASSWORD, value, RegistryValueKind.String);
                }

                _encryptedPrivateKeyPassword = value;
            }
        }

        public IEnumerable<string> Claims {
            get {
                return Enabled ? _claims : Enumerable.Empty<string>();
            }
        }

        public string Name { get; private set; }

        public async Task<ICertificate> GetCertificate(string id)
        {
            EnsureAccess(CertificateAccess.Read);
            CertificateIdentifier certId = CertificateIdentifier.Parse(id);

            IEnumerable<ICertificate> certs = await GetCertificates();

            foreach (var cert in certs) {
                if (cert.Thumbprint.Equals(certId.Id) && cert.Alias.Equals(certId.Name, StringComparison.OrdinalIgnoreCase)) {
                    return cert;
                }
            }

            return null;
        }


        public async Task<IEnumerable<ICertificate>> GetCertificates()
        {
            EnsureAccess(CertificateAccess.Read);
            List<ICertificate> certificates = null;

            try {
                var certs = await GetCerts();
                var l = new List<ICertificate>();

                foreach (X509Certificate2 cert in certs) {
                    l.Add(new Certificate(cert, this, new CertificateIdentifier(cert).Id));
                    cert.Dispose();
                }

                certificates = l;
            }
            catch (Win32Exception) {
            }
            catch (IOException) {
            }

            return certificates ?? Enumerable.Empty<ICertificate>();
        }

        public Stream GetContent(ICertificate certificate, bool persistKey, string password)
        {
            throw new NotImplementedException();
        }

        private async Task<IEnumerable<X509Certificate2>> GetCerts()
        {
            return (await CentralCertHelper.GetFiles()).Select(f => {

                X509Certificate2 cert = !string.IsNullOrEmpty(EncryptedPrivateKeyPassword) ?
                                            new X509Certificate2(f, Crypto.Decrypt(Convert.FromBase64String(EncryptedPrivateKeyPassword))) :
                                            new X509Certificate2(f);

                cert.FriendlyName = Path.GetFileName(f);
                return cert;
            });
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

        private void EnsureRefreshed()
        {
            if (DateTime.UtcNow - _lastRefresh > TimeSpan.FromSeconds(DEFAULT_POLLING_INTERVAL)) {
                Refresh();
            }
        }

        private void Refresh()
        {
            _lastRefresh = DateTime.UtcNow;
            using (RegistryKey key = GetRegKey(false)) {
                _enabled = (int)(key.GetValue(REGVAL_ENABLED, 0)) != 0;
                _physicalPath = key.GetValue(REGVAL_CERT_STORE_LOCATION, null) as string;
                _username = key.GetValue(REGVAL_USERNAME, null) as string;
                _pollingInterval = (int)key.GetValue(REGVAL_POLLING_INTERVAL, DEFAULT_POLLING_INTERVAL);
                _encryptedPassword = key.GetValue(REGVAL_PASSWORD, null) as string;
                _encryptedPrivateKeyPassword = key.GetValue(REGVAL_PRIVATE_KEY_PASSWORD, null) as string;
            }
        }

        //
        // IDisposable
        private RegistryKey GetRegKey(bool writable = true)
        {
            return Registry.LocalMachine.OpenSubKey(REGKEY_CENTRAL_CERTIFICATE_STORE_PROVIDER, writable);
        }
    }
}
