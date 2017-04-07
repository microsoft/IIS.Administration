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

    public class CentralCertificateStore : ICertificateStore
    {
        private IEnumerable<string> _claims;
        private DateTime _lastRefresh;
        private bool _enabled;
        private int _pollingInterval;
        private string _physicalPath;
        private string _username;
        private string _encryptedPassword;
        private string _encryptedPrivateKeyPassword;

        public CentralCertificateStore(IEnumerable<string> claims)
        {
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
                        key.DeleteValue(Constants.REGVAL_PASSWORD, false);
                        key.DeleteValue(Constants.REGVAL_PRIVATE_KEY_PASSWORD, false);
                    }

                    key.SetValue(Constants.REGVAL_ENABLED, value ? 1 : 0);
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
                    key.SetValue(Constants.REGVAL_POLLING_INTERVAL, value);
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
                    key.SetValue(Constants.REGVAL_CERT_STORE_LOCATION, value, RegistryValueKind.String);
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
                    key.SetValue(Constants.REGVAL_USERNAME, value, RegistryValueKind.String);
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
                    key.SetValue(Constants.REGVAL_PASSWORD, Convert.ToBase64String(Crypto.Encrypt(value)), RegistryValueKind.String);
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
                    key.SetValue(Constants.REGVAL_PRIVATE_KEY_PASSWORD, Convert.ToBase64String(Crypto.Encrypt(value)), RegistryValueKind.String);
                }

                _encryptedPrivateKeyPassword = value;
            }
        }

        public bool IsWindowsStore {
            get {
                return false;
            }
        }

        public IEnumerable<string> Claims {
            get {
                return Enabled ? _claims : Enumerable.Empty<string>();
            }
        }

        public string Name {
            get {
                return Constants.STORE_NAME;
            }
        }

        public async Task<ICertificate> GetCertificate(string thumbprint)
        {
            EnsureAccess(CertificateAccess.Read);
            IEnumerable<ICertificate> certs = await GetCertificates();

            foreach (var cert in certs) {
                if (cert.Thumbprint.Equals(thumbprint)) {
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
                var certs = await CentralCertHelper.GetCertificates();
                var l = new List<ICertificate>();

                foreach (X509Certificate2 cert in certs) {
                    l.Add(new Certificate(cert, this));
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

        //
        // IDisposable
        public Stream GetContent(ICertificate certificate, bool persistKey, string password)
        {
            EnsureAccess(CertificateAccess.Read | CertificateAccess.Export);

            X509Certificate2 target = null;
            Stream stream = null;

            foreach (X509Certificate2 cert in CentralCertHelper.GetCertificates().Result) {
                if (cert.Thumbprint.Equals(certificate.Thumbprint)) {
                    target = cert;
                }
                else {
                    cert.Dispose();
                }
            }

            if (target != null && !persistKey) {
                byte[] bytes = password == null ? target.Export(X509ContentType.Cert) : target.Export(X509ContentType.Cert, password);
                stream = new MemoryStream(bytes);
                target.Dispose();
            }

            return stream;
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
                throw new ForbiddenArgumentException("certificate_store");
            }
        }

        private void EnsureRefreshed()
        {
            if (DateTime.UtcNow - _lastRefresh > TimeSpan.FromSeconds(Constants.DEFAULT_POLLING_INTERVAL)) {
                Refresh();
            }
        }

        private void Refresh()
        {
            _lastRefresh = DateTime.UtcNow;
            using (RegistryKey key = GetRegKey(false)) {
                _enabled = (int)(key.GetValue(Constants.REGVAL_ENABLED, 0)) != 0;
                _physicalPath = key.GetValue(Constants.REGVAL_CERT_STORE_LOCATION, null) as string;
                _username = key.GetValue(Constants.REGVAL_USERNAME, null) as string;
                _pollingInterval = (int)key.GetValue(Constants.REGVAL_POLLING_INTERVAL, Constants.DEFAULT_POLLING_INTERVAL);
                _encryptedPassword = key.GetValue(Constants.REGVAL_PASSWORD, null) as string;
                _encryptedPrivateKeyPassword = key.GetValue(Constants.REGVAL_PRIVATE_KEY_PASSWORD, null) as string;
            }
        }

        //
        // IDisposable
        private RegistryKey GetRegKey(bool writable = true)
        {
            return Registry.LocalMachine.OpenSubKey(Constants.REGKEY_CENTRAL_CERTIFICATE_STORE_PROVIDER, writable);
        }
    }
}
