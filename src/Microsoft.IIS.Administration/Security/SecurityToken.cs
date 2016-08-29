// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Security {
    using System;
    using Core.Security;
    using IdentityModel.Tokens;

    class SecurityToken : IdentityModel.Tokens.SecurityToken {
        private ApiKey _key;

        public SecurityToken(ApiKey key) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            _key = key;
        }

        public override string Id {
            get {
                return _key.Id;
            }
        }

        public override string Issuer {
            get {
                return "localhost";
            }
        }

        public override SecurityKey SecurityKey {
            get {
                throw new NotImplementedException();
            }
        }

        public override SecurityKey SigningKey {
            get {
                throw new NotImplementedException();
            }

            set {
                throw new NotImplementedException();
            }
        }

        public override DateTime ValidFrom {
            get {
                return _key.CreatedOn;
            }
        }

        public override DateTime ValidTo {
            get {
                return _key.ExpiresOn ?? DateTime.MaxValue;
            }
        }
    }
}
