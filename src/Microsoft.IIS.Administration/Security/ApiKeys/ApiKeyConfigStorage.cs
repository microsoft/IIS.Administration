// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Security {
    using Microsoft.IIS.Administration.Core.Security;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System;
    using Microsoft.IIS.Administration.Core.Utils;
    using System.Security.Cryptography;
    using System.Linq;
    using Microsoft.Extensions.Configuration;
    using System.Threading;



    /*
      Example:
          /api_keys:keys:0:token_hash="<hash>"
    */

    class ApiKeyConfigStorage : IApiKeyStorage {
        private IConfiguration _config;
        private IDictionary<string, ApiKey> _keys; // Read-onlya 


        public ApiKeyConfigStorage(IConfiguration config) {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public Task<IEnumerable<ApiKey>> GetAllKeys() {
            EnsureInit();

            return Task.FromResult<IEnumerable<ApiKey>>(_keys.Values);
        }

        public Task<ApiKey> GetKeyByHash(string hash) {
            EnsureInit();

            ApiKey apiKey = null;

            if (!_keys.TryGetValue(hash, out apiKey)) {
                apiKey = null;
            }

            return Task.FromResult(apiKey);
        }


        public Task<ApiKey> GetKeyById(string id) {
            EnsureInit();

            return Task.FromResult(_keys.Values.Where(k => k.Id == id).FirstOrDefault());
        }

        public Task SaveKey(ApiKey key) {
            var existing = _keys.Where(kv => kv.Value.Id == key.Id).FirstOrDefault();

            if (existing.Key == null) {
                //
                // Don't support adding new api keys. Allow update of existing though
                return Task.CompletedTask;
            }

            //
            // Do copy on write
            IDictionary<string, ApiKey> copy = _keys.ToDictionary(kv => kv.Key, kv => kv.Value);

            // Remove the existing
            copy.Remove(existing.Key);

            copy.Add(key.TokenHash, key);

            Interlocked.Exchange(ref _keys, copy); // To avoid volatile

            return Task.CompletedTask;
        }

        public async Task<bool> RemoveKey(ApiKey key) {
            if (await GetKeyByHash(key.TokenHash) == null) {
                return false;
            }

            //
            // Do copy on write
            IDictionary<string, ApiKey> copy = _keys.ToDictionary(kv => kv.Key, kv => kv.Value);
            copy.Remove(key.TokenHash);

            Interlocked.Exchange(ref _keys, copy); // To avoid volatile

            return true;
        }


        private void EnsureInit() {
            if (_keys == null) {
                _keys = Load();
            }
        }

        private IDictionary<string, ApiKey> Load() {
            var keys = new Dictionary<string, ApiKey>();
            var keysSection = _config?.GetSection("api_keys:keys")?.GetChildren();

            if (keysSection != null) {
                foreach (var k in keysSection) {
                    ApiKey key = FromConfig(k);
                    keys[key.TokenHash] = key;
                }
            }

            return keys;
        }

        private static ApiKey FromConfig(IConfigurationSection config) {
            var key = new ApiKey(config.GetValue<string>("token_hash"), config.GetValue<string>("token_type") ?? "SWT") {
                Id = config.GetValue<string>("id") ?? GenerateId(),
                Purpose = config.GetValue<string>("purpose") ?? string.Empty,
                CreatedOn = config.GetValue<DateTime>("created_on"),
                ExpiresOn = config.GetValue<DateTime>("expires_on"),
                LastModified = config.GetValue<DateTime>("last_modified")
            };

            if (key.CreatedOn == DateTime.MinValue) {
                key.CreatedOn = DateTime.UtcNow;
            }

            if (key.LastModified == DateTime.MinValue) {
                key.LastModified = key.CreatedOn;
            }

            if (key.ExpiresOn == DateTime.MinValue) {
                key.ExpiresOn = null;
            }

            return key;
        }

        private static string GenerateId() {
            return Base64.Encode(GenerateRandom(16));
        }

        private static byte[] GenerateRandom(int bytesLen) {
            byte[] bytes = new byte[bytesLen];

            using (var rng = RandomNumberGenerator.Create()) {
                rng.GetBytes(bytes);
            }

            return bytes;
        }
    }
}
