// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Microsoft.IIS.Administration.Core;
    using Microsoft.IIS.Administration.WebServer.Utils;
    using Microsoft.Win32;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    class UrlRewriteFeatureManager
    {
        private const string DOWNLOAD_URL = "https://go.microsoft.com/fwlink/?linkid=853092";
        private const string REGKEY_INSTALLED_PRODUCTS = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        private const string REGKEY_URL_REWRITE_INSTALLED = @"SOFTWARE\Microsoft\IIS Extensions\URL Rewrite";

        public bool IsInstalled()
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(REGKEY_URL_REWRITE_INSTALLED, false)) {
                return key != null;
            }
        }

        public Version GetVersion()
        {
            Version version = null;

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(REGKEY_URL_REWRITE_INSTALLED, false)) {
                if (key != null) {
                    version = Version.Parse((string)key.GetValue("Version"));
                }
            }

            return version;
        }

        public async Task Install()
        {
            string tempPath = Path.GetTempPath() + Guid.NewGuid().ToString();
            string fileName = "rewrite_amd64.msi";

            if (!Directory.Exists(tempPath)) {
                Directory.CreateDirectory(tempPath);
            }

            string downloadPath = Path.Combine(tempPath, fileName);

            try {

                //
                // Download
                using (var client = new HttpClient())
                using (Stream fileStream = new FileStream(downloadPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read)) {

                    Stream responseStream = await client.GetStreamAsync(DOWNLOAD_URL);

                    await responseStream.CopyToAsync(fileStream);
                }

                //
                // Verify
                if (!IsInstallerValid(downloadPath)) {
                    throw new ApiException("Download failed", fileName, null);
                }

                //
                // Run installer
                ProcessStartInfo info = new ProcessStartInfo("msiexec.exe", $"/i {downloadPath} /quiet /norestart");
                await RunInstaller(info);
            }
            finally {
                Directory.Delete(tempPath, true);
            }
        }

        public async Task Uninstall()
        {
            string productGuid = GetProductGuid();

            if (productGuid == null) {
                throw new FeatureNotFoundException(RewriteHelper.DISPLAY_NAME);
            }

            ProcessStartInfo info = new ProcessStartInfo("msiexec.exe", $"/x {productGuid} /quiet /norestart");
            await RunInstaller(info);
        }

        private string GetProductGuid()
        {
            using (var key = Registry.LocalMachine.OpenSubKey(REGKEY_INSTALLED_PRODUCTS, false)) {

                foreach (string name in key.GetSubKeyNames()) {

                    using (var subkey = key.OpenSubKey(name)) {
                        string displayName = (string)subkey.GetValue("DisplayName");

                        if (displayName != null && displayName.Contains("IIS URL Rewrite Module")) {
                            return name;
                        }
                    }
                }
            }

            return null;
        }

        private bool IsInstallerValid(string path)
        {
            X509Certificate2 cert = CertificateUtility.CreateCertificateFromFile(path);

            //
            // Only use Microsoft signed MSI
            if (cert.Subject.Contains("O=Microsoft Corporation,")) {
                var chain = new X509Chain();

                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                chain.ChainPolicy.VerificationFlags |= X509VerificationFlags.IgnoreNotTimeValid;

                return chain.Build(cert);
            }

            return false;
        }

        private Task<int> RunInstaller(ProcessStartInfo info)
        {
            Process p = new Process() {
                StartInfo = info,
                EnableRaisingEvents = true
            };

            var tcs = new TaskCompletionSource<int>();

            p.Exited += (sender, args) => {
                if (p.ExitCode != 0) {
                    tcs.SetException(new InstallationException(p.ExitCode, RewriteHelper.DISPLAY_NAME));
                }
                else {
                    tcs.SetResult(p.ExitCode);
                }
                p.Dispose();
            };

            p.Start();
            return tcs.Task;
        }
    }
}
