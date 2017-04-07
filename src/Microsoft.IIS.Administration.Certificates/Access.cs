namespace Microsoft.IIS.Administration.Certificates
{
    using System;

    [Flags]
    public enum CertificateAccess
    {
        Read = 1,
        Delete = 2,
        Create = 4,
        Export = 8
    }
}
