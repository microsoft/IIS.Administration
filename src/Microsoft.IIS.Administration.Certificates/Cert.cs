namespace Microsoft.IIS.Administration.Certificates
{
    using System;
    using System.Security.Cryptography.X509Certificates;

    public sealed class Cert : IDisposable
    {
        public X509Certificate2 Certificate { get; set; }
        public X509Store Store { get; set; }

        public void Dispose()
        {
            if (Certificate != null) {
                Certificate.Dispose();
                Certificate = null;
            }
        }
    }
}
