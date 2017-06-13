// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Security.Authorization {

    sealed class AccessPolicy {
        public string Users { get; set; } = string.Empty;
        public bool AccessKey { get; set; } = true;
        public bool Forbidden { get; set; }
        public bool ReadOnly { get; set; }
    }
}
