// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.AccessManagement {
    using System;
    using Core.Security;


    internal static class AccessTokenHelper {

        public static dynamic ToJsonModel(ApiToken token) {
            if (token.Key == null) {
                throw new ArgumentNullException(nameof(token.Key));
            }

            if (string.IsNullOrEmpty(token.Token)) {
                throw new ArgumentNullException(nameof(token.Token));
            }

            dynamic obj = new {
                id = token.Key.Id,
                created_on = DateTime.UtcNow,
                expires_on = (object)token.Key.ExpiresOn ?? string.Empty,
                value = token.Token,
                type = token.Key.TokenType,
                api_key = ApiKeyHelper.ToJsonModelRef(token.Key)
            };

            return Core.Environment.Hal.Apply(Defines.AccessTokensResource.Guid, obj, true);
        }

        public static dynamic ToJsonModel(ApiKey key) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            dynamic obj = new {
                id = key.Id,
                expires_on = (object)key.ExpiresOn ?? string.Empty,
                type = key.TokenType,
                api_key = ApiKeyHelper.ToJsonModelRef(key)
            };

            return Core.Environment.Hal.Apply(Defines.AccessTokensResource.Guid, obj, true);
        }


        public static string GetLocation(string id) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentNullException(nameof(id));
            }

            return $"/{Defines.ACCESSTOKENS_PATH}/{id}";
        }
    }
}
