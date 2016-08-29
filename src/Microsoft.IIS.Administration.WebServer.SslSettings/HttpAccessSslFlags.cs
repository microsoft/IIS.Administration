// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.SslSettings
{
    using System;

    [Flags]
        public enum HttpAccessSslFlags {
            None = 0x00000000,
            Ssl = 0x00000008,
            SslNegotiateCert = 0x00000020,
            SslRequireCert = 0x00000040,
            SslMapCert = 0x00000080,
            Ssl128 = 0x00000100,
        }
}
