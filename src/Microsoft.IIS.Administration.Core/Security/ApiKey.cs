// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core.Security {
    using System;

    public struct ApiToken {
        public string Token;
        public ApiKey Key;
    }


    public class ApiKey {
        private string _tokenHash;

        public ApiKey(string tokenHash, string tokenType) {
            TokenHash = tokenHash;
            TokenType = tokenType ?? string.Empty;
        }

        public string Id { get; set; }

        public string Purpose { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime? ExpiresOn { get; set; }

        public DateTime LastModified { get; set; }

        public string TokenHash {
            get {
                return _tokenHash;
            }

            set {
                if (string.IsNullOrEmpty(value)) {
                    throw new ArgumentNullException();
                }

                _tokenHash = value;
            }
        }

        public string TokenType { get; set; }
    }
}
