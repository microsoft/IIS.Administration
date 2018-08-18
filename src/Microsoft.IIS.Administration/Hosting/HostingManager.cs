// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration
{
    using Microsoft.Extensions.Configuration;
    using Serilog;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    class HostingManager
    {
        const string IISAdminCertName = "Microsoft IIS Administration Server Certificate";
        private IConfiguration _config;

        public HostingManager(IConfiguration configuration)
        {
            this._config = configuration;
        }

        public async Task Initialize()
        {
            // Check configuration to see if certificate should be created and bound, or an existing certificate should be bound

            //
            // Read configuration
            bool generateCertificate = _config.GetValue<bool>("hosting:generateCertificate");
            string thumbprint = _config.GetValue<string>("hosting:certificate:thumbprint");
            string urls = _config.GetValue<string>("urls", "https://*:55539");

            if (!generateCertificate && string.IsNullOrEmpty(thumbprint))
            {
                //
                // Nothing to do
                return;
            }

            string ipPort = urls.Substring(urls.IndexOf("//") + "//".Length);

            //
            // Obtain target IP + port
            if (!ipPort.Contains(":"))
            {
                Log.Fatal($"Invalid urls value in application settings: {urls}");

                throw new Exception("Invalid configuration");
            }

            string ipStr = ipPort.Substring(0, ipPort.IndexOf(':'));

            string portStr = ipPort.Substring(ipStr.Length + 1);

            if (IPAddress.TryParse(ipStr, out IPAddress ip))
            {
                // No op
            }
            else if (ipStr == "*")
            {
                ip = IPAddress.Any;
            }
            else
            {
                Log.Fatal($"Invalid urls value in application settings: {urls}");

                throw new Exception("Invalid configuration");
            }

            if (!int.TryParse(portStr, out int port) || port > ushort.MaxValue || port < 0)
            {
                Log.Fatal($"Invalid urls value in application settings: {urls}");

                throw new Exception("Invalid configuration");
            }

            var endpoint = new IPEndPoint(ip, port);

            var info = Netsh.GetSslBinding(endpoint);

            if (info != null)
            {
                //
                // Binding info already exists
                // We don't override existing bindings

                //
                // Warn if certificate used in the existing binding doesn't match the desired certificate
                if (!string.IsNullOrEmpty(thumbprint) && !BitConverter.ToString(info.CertificateHash).Replace("-", string.Empty).Equals(thumbprint, StringComparison.OrdinalIgnoreCase))
                {
                    Log.Warning("Ssl certificate already registered on target port");
                }

                return;
            }

            X509Certificate2 certificate = null;

            if (generateCertificate)
            {
                //
                // Use a previously generated certificate if any exist that are not expired
                List<X509Certificate2> previousCerts = new List<X509Certificate2>();

                using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
                {
                    store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);

                    foreach (var cert in store.Certificates)
                    {
                        if (cert.FriendlyName.Contains(IISAdminCertName))
                        {
                            previousCerts.Add(cert);
                        }
                        else
                        {
                            cert.Dispose();
                        }
                    }
                }

                if (previousCerts.Count > 0)
                {
                    var newest = previousCerts[0];

                    foreach (var cert in previousCerts)
                    {
                        if (cert.NotAfter > newest.NotAfter)
                        {
                            newest = cert;
                        }
                    }

                    if (newest.NotAfter > DateTime.UtcNow)
                    {
                        certificate = newest;
                    }
                }


                if (certificate == null)
                {
                    //
                    // Generate a new IIS Admin cert if not a valid existing one
                    certificate = await CertificateHelper.Create(
                        "localhost",
                        previousCerts.Count > 0 ? IISAdminCertName + " " + DateTime.Now.Year : IISAdminCertName,
                        new string[] { "localhost", Environment.MachineName }
                    );
                }

                //
                // Dispose unused certs
                foreach (var cert in previousCerts)
                {
                    if (cert != certificate)
                    {
                        cert.Dispose();
                    }
                }
            }
            else if (!string.IsNullOrEmpty(thumbprint))
            {
                using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
                {
                    store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);

                    foreach (var cert in store.Certificates)
                    {
                        if (cert.Thumbprint.Equals(thumbprint, StringComparison.OrdinalIgnoreCase))
                        {
                            certificate = cert;

                            break;
                        }
                        else
                        {
                            cert.Dispose();
                        }
                    }
                }
            }

            //
            // Register target certificate on ip/port
            if (certificate != null)
            {
                Netsh.SetHttpsBinding(endpoint, certificate, Guid.Parse("4dc3e181-e14b-4a21-b022-59fc669b0914"));
            }
        }
    }
}
