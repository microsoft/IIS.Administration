namespace Microsoft.IIS.Administration.Certificates
{
    using System;
    using System.Security.Cryptography.X509Certificates;

    public sealed class Cert : IDisposable
    {
        public X509Certificate2 Certificate { get; set; }
        public string StoreName { get; set; }
        public StoreLocation StoreLocation { get; set; }

        public Cert(X509Certificate2 certificate, string storeName, StoreLocation storeLocation)
        {
            Certificate = certificate;
            StoreName = storeName;
            StoreLocation = storeLocation;
        }

        public void Dispose()
        {
            if (Certificate != null) {
                Certificate.Dispose();
                Certificate = null;
            }
        }
    }
}
