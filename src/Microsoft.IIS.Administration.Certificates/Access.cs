namespace Microsoft.IIS.Administration.Certificates
{
    using System;

    [Flags]
    public enum Access
    {
        Read = 1,
        Write = 2,
        Export = 4
    }
}
