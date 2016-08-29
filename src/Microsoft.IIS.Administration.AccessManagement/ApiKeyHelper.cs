// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.AccessManagement {
    using System;
    using Core;
    using Core.Security;
    using Core.Utils;


    internal static class ApiKeyHelper {

        public static void Update(ApiKey key, dynamic model) {
            if (key == null) {
                throw new ArgumentNullException();
            }

            if (model == null) {
                throw new ApiArgumentException("model");
            }

            //
            // purpose
            string purpose = DynamicHelper.Value(model.purpose);
            if (purpose != null) {
                key.Purpose = purpose;
            }

            //
            // expires_on
            DateTime? expiresOn = DynamicHelper.To<DateTime>(model.expires_on);
            if (expiresOn != null) {
                key.ExpiresOn = expiresOn;
            }

            //
            // Set last modified
            key.LastModified = DateTime.Now;
        }

        public static dynamic ToJsonModel(ApiToken token) {
            var key = token.Key;

            dynamic obj = new {
                purpose = key.Purpose,
                id = key.Id,
                created_on = key.CreatedOn,
                last_modified = key.LastModified,
                expires_on = (object)key.ExpiresOn ?? string.Empty,
                access_token = token.Token
            };

            return Core.Environment.Hal.Apply(Defines.ApiKeysResource.Guid, obj, true);
        }

        public static dynamic ToJsonModel(ApiKey key) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            dynamic obj = new {
                purpose = key.Purpose,
                id = key.Id,
                created_on = key.CreatedOn,
                last_modified = key.LastModified,
                expires_on = (object)key.ExpiresOn ?? string.Empty
            };

            return Core.Environment.Hal.Apply(Defines.ApiKeysResource.Guid, obj, true);
        }

        public static dynamic ToJsonModelRef(ApiKey key) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            dynamic obj = new {
                purpose = key.Purpose,
                id = key.Id
            };

            return Core.Environment.Hal.Apply(Defines.ApiKeysResource.Guid, obj, false);
        }


        public static string GetLocation(string id) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentNullException(nameof(id));
            }

            return $"/{Defines.APIKEYS_PATH}/{id}";
        }
    }
}
