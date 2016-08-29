// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core {
    using System;

    public static class Globals {
        // Root of all api paths
        // Access Token IS available 
        public static readonly string API_PATH = "api";

        // Root of all SECURITY areas
        // The Host MUST maintain integrated security (Windows Auth, Client Cert, etc.)
        // Access Token IS NOT available!
        // CORs MUST be explicitly disabled
        // AntiForgery MUST be applied
        public static readonly string SECURITY_PATH = "security";

        public static readonly ResDef ApiResource = new ResDef("api", new Guid("76049EA6-FFB7-415E-9E3D-FC6F03414663"), API_PATH);


        public static readonly string PING_PATH = $"{API_PATH}/ping";
    }
}
