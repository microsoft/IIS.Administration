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
    using Win32;
    using Win32.SafeHandles;
    using System.Runtime.InteropServices;
    using Files;
    using Extensions.Caching.Memory;

    sealed class CentralCertificateStore : ICertificateStore, ICentralCertificateStore, IDisposable
    {
        private const string REGKEY_CENTRAL_CERTIFICATE_STORE_PROVIDER = "SOFTWARE\\Microsoft\\IIS\\CentralCertProvider";
        private const string REGVAL_CERT_STORE_LOCATION = "CertStoreLocation";
        private const string REGVAL_USERNAME = "UserName";
        private const string REGVAL_PASSWORD = "Password";
        private const string REGVAL_PRIVATE_KEY_PASSWORD = "PrivateKeyPassword";
        private const string REGVAL_POLLING_INTERVAL = "PollingInterval";
        private const string REGVAL_ENABLED = "Enabled";
        private const int DEFAULT_POLLING_INTERVAL = 300; // seconds
        private const string CERTS_KEY = "certs";
        private MemoryCache _cache;
        private FileSystemWatcher _watcher;
        private readonly IEnumerable<string> _claims;
        private IFileProvider _fileProvider;
        private DateTime _lastRefresh;
        private bool _enabled;
        private int _pollingInterval;
        private string _physicalPath;
        private string _username;
        private string _encryptedPassword;
        private string _encryptedPrivateKeyPassword;

        public CentralCertificateStore(string name, IEnumerable<string> claims, IFileProvider fileProvider)
        {
            Name = name;
            _claims = claims;
            _fileProvider = fileProvider;
            _cache = new MemoryCache(new MemoryCacheOptions() {
                ExpirationScanFrequency = TimeSpan.FromSeconds(30)
            });
            
            //
            // File System Watcher, initially disabled
            _watcher = new FileSystemWatcher();
            _watcher.Changed += OnChanged;
            _watcher.Created += OnChanged;
            _watcher.Deleted += OnChanged;
            _watcher.Renamed += OnChanged;

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
                _cache.Remove(CERTS_KEY);
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
                _cache.Remove(CERTS_KEY);
            }
        }

        public string Password {
            get {
                EnsureRefreshed();
                return Crypto.Decrypt(Convert.FromBase64String(_encryptedPassword));
            }
            set {
                string encrypted = Convert.ToBase64String(Crypto.Encrypt(value));

                using (RegistryKey key = GetRegKey()) {
                    key.SetValue(REGVAL_PASSWORD, encrypted, RegistryValueKind.String);
                }

                _encryptedPassword = encrypted;
                _cache.Remove(CERTS_KEY);
            }
        }

        public string PrivateKeyPassword {
            get {
                EnsureRefreshed();
                return string.IsNullOrEmpty(_encryptedPrivateKeyPassword) ? string.Empty : Crypto.Decrypt(Convert.FromBase64String(_encryptedPrivateKeyPassword));
            }
            set {
                string encrypted = Convert.ToBase64String(Crypto.Encrypt(value));

                using (RegistryKey key = GetRegKey()) {
                    key.SetValue(REGVAL_PRIVATE_KEY_PASSWORD, encrypted, RegistryValueKind.String);
                }

                _encryptedPrivateKeyPassword = encrypted;
                _cache.Remove(CERTS_KEY);
            }
        }

        public IEnumerable<string> Claims {
            get {
                return Enabled ? _claims : Enumerable.Empty<string>();
            }
        }

        public string Name { get; private set; }

        public void Dispose()
        {
            if (_watcher != null) {
                _watcher.Dispose();
                _watcher = null;
            }

            if (_cache != null) {
                _cache.Dispose();
                _cache = null;
            }
        }

        // ICentralCertificateStore.GetCertificateByHostName
        // Prefer ICertificateStore.GetCertificate if possible
        public async Task<ICertificate> GetCertificateByHostName(string name)
        {
            EnsureAccess(CertificateAccess.Read);
            ICertificate certificate = null;

            IEnumerable<ICertificate> certs = await GetCertificatesInternal(name);

            foreach (var cert in certs) {
                if (certificate == null && Path.GetFileNameWithoutExtension(cert.Alias).Equals(name, StringComparison.OrdinalIgnoreCase)) {
                    certificate = cert;
                    break;
                }
            }

            return certificate;
        }

        public async Task<ICertificate> GetCertificate(string fileName)
        {
            EnsureAccess(CertificateAccess.Read);
            ICertificate certificate = null;

            IEnumerable<ICertificate> certs = await GetCertificatesInternal(Path.GetFileNameWithoutExtension(fileName));

            foreach (var cert in certs) {
                if (cert.Alias.Equals(fileName, StringComparison.OrdinalIgnoreCase)) {
                    certificate = cert;
                    break;
                }
            }

            return certificate;
        }

        public async Task<IEnumerable<ICertificate>> GetCertificates()
        {
            EnsureAccess(CertificateAccess.Read);
            IEnumerable<ICertificate> cached = _cache.Get<IEnumerable<ICertificate>>(CERTS_KEY);

            if (cached != null) {
                return cached;
            }

            var certs = await GetCertificatesInternal(null);
            _cache.Set(CERTS_KEY, certs, TimeSpan.FromSeconds(DEFAULT_POLLING_INTERVAL));
            return certs;
        }

        private async Task<IEnumerable<ICertificate>> GetCertificatesInternal(string name)
        {
            var pswd = PrivateKeyPassword;
            var certs = new List<ICertificate>();
            IEnumerable<IFileInfo> files = null;

            try {
                files = await GetFiles(name);

            }
            catch (Win32Exception) {
                throw new ForbiddenArgumentException("certificate_store", "Invalid central certificate store credentials", Name);
            }
            catch (ForbiddenArgumentException e) {
                throw new ForbiddenArgumentException("certificate_store", "Cannot access store", Name, e);
            }

            foreach (var file in files) {
                certs.Add(new Certificate(file, this, _fileProvider, pswd));
            }
            return certs;
        }

        private async Task<IEnumerable<IFileInfo>> GetFiles(string filter = null)
        {
            var ccs = Startup.CentralCertificateStore;

            if (!string.IsNullOrEmpty(filter) && !PathUtil.IsValidFileName(filter)) {
                throw new ArgumentException(nameof(filter));
            }

            return await Task.Run(() => {
                IFileInfo ccsDir = _fileProvider.GetDirectory(ccs.PhysicalPath);
                return _fileProvider.GetFiles(ccsDir, string.IsNullOrEmpty(filter) ? ("*.pfx") : (filter + ".pfx"), SearchOption.TopDirectoryOnly);
            });
        }

        public Stream GetContent(ICertificate certificate, bool persistKey, string password)
        {
            throw new NotImplementedException();
        }

        internal static SafeAccessTokenHandle LogonUser(string username, string password)
        {
            SafeAccessTokenHandle token = null;

            string[] parts = username.Split('\\');
            string domain = null;

            if (parts.Length > 1) {
                domain = parts[0];
                username = parts[1];
            }
            else {
                domain = ".";
                username = parts[0];
            }

            bool loggedOn = Interop.LogonUserExExW(username,
                domain,
                password,
                Interop.LOGON32_LOGON_INTERACTIVE,
                Interop.LOGON32_PROVIDER_DEFAULT,
                IntPtr.Zero,
                out token,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);

            if (!loggedOn) {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return token;
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
                if (key == null) {
                    _enabled = false;
                    _physicalPath = null;
                    _username = null;
                    _pollingInterval = DEFAULT_POLLING_INTERVAL;
                    _encryptedPassword = null;
                    _encryptedPrivateKeyPassword = null;
                }
                else {
                    _enabled = (int)(key.GetValue(REGVAL_ENABLED, 0)) != 0;
                    _physicalPath = key.GetValue(REGVAL_CERT_STORE_LOCATION, null) as string;
                    _username = key.GetValue(REGVAL_USERNAME, null) as string;
                    _pollingInterval = (int)key.GetValue(REGVAL_POLLING_INTERVAL, DEFAULT_POLLING_INTERVAL);
                    _encryptedPassword = key.GetValue(REGVAL_PASSWORD, null) as string;
                    _encryptedPrivateKeyPassword = key.GetValue(REGVAL_PRIVATE_KEY_PASSWORD, null) as string;
                }
            }
            SetupWatcher();
        }

        private void SetupWatcher()
        {
            if (!Enabled) {
                _watcher.EnableRaisingEvents = false;
                return;
            }

            _watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size;

            //
            // If system does not have access to CCS path we cannot watch for changes, but this is not an invalid scenario
            // We won't enable the watcher
            try {
                _watcher.Path = PhysicalPath;
                _watcher.EnableRaisingEvents = true;
            }
            catch (ArgumentException) {
                _watcher.EnableRaisingEvents = false;
            }
            catch (FileNotFoundException) {
                _watcher.EnableRaisingEvents = false;
            }
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            this._cache.Remove(CERTS_KEY);
        }

        //
        // IDisposable
        private RegistryKey GetRegKey(bool writable = true)
        {
            return Registry.LocalMachine.OpenSubKey(REGKEY_CENTRAL_CERTIFICATE_STORE_PROVIDER, writable);
        }
    }
}
