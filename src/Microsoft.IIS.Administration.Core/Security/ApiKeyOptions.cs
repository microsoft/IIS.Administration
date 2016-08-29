// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core.Security {

    public class ApiKeyOptions {
        public int KeySize { get; set; } = 32; // (in bytes)
        public int SaltSize { get; set; } = 8;  // (in bytes)
        public int HashSize { get; set; } = 32; // (in bytes)
    }
}
