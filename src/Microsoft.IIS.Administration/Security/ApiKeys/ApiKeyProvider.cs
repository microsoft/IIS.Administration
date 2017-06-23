// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Security {
    using Core.Security;
    using Core.Utils;
    using Extensions.Caching.Memory;
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Threading.Tasks;


    sealed class ApiKeyProvider : IApiKeyProvider {
        private ApiKeyOptions _options;
        private MemoryCache _tokenCache;
        private static readonly TimeSpan TokenCacheExpiration = TimeSpan.FromSeconds(30);

        private List<IApiKeyStorage> _storages = new List<IApiKeyStorage>();


        public ApiKeyProvider(ApiKeyOptions options) {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            ResetCache();
        }


        public void UseStorage(IApiKeyStorage storage) {
            _storages.Add(storage ?? throw new ArgumentNullException(nameof(storage)));
        }

        public ApiToken GenerateKey(string purpose) {
            byte[] salt = GenerateRandom(_options.SaltSize);  // Salt used for hashing
            byte[] key = GenerateRandom(_options.KeySize);    // ApiKey value
            byte[] hmac = CalcHash(key, salt);                // HMAC of the ApiKey w/ Salt

            //
            // Create api-key value
            // [Salt][Key]
            //
            byte[] saltAndKey = new byte[salt.Length + key.Length];

            Buffer.BlockCopy(salt, 0, saltAndKey, 0, salt.Length);
            Buffer.BlockCopy(key, 0, saltAndKey, salt.Length, key.Length);

            //
            // ApiKey
            ApiKey apiKey = new ApiKey(Base64.Encode(hmac), "SWT");
            apiKey.Id = GenerateId();
            apiKey.Purpose = purpose ?? "";
            apiKey.CreatedOn = DateTime.UtcNow;
            apiKey.LastModified = apiKey.CreatedOn;

            return new ApiToken {
                Token = Base64.Encode(saltAndKey),
                Key = apiKey
            };
        }

        public async Task<string> RenewToken(ApiKey key) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            ResetCache();

            string oldHash = key.TokenHash;
            ApiToken apiToken = GenerateKey(key.Purpose);

            key.TokenHash = apiToken.Key.TokenHash;
            key.TokenType = apiToken.Key.TokenType;
            key.LastModified = DateTime.UtcNow;

            //
            // Save
            foreach (var s in _storages) {
                if (await s.GetKeyByHash(oldHash) != null) {
                    await s.SaveKey(key);
                }
            }

            return apiToken.Token;
        }

        public ApiKey FindKey(string token) {
            if (string.IsNullOrWhiteSpace(token)) {
                return null;
            }

            ApiKey apiKey = null;

            if (!_tokenCache.TryGetValue(token, out apiKey)) {
                byte[] saltAndKey = Base64.Decode(token);
                if (saltAndKey.Length != (_options.SaltSize + _options.KeySize)) {
                    // Invalid length
                    return null;
                }

                //
                // Split the salt and key
                // [Salt][Key]
                //
                byte[] salt = new byte[_options.SaltSize];
                byte[] key = new byte[_options.KeySize];

                Buffer.BlockCopy(saltAndKey, 0, salt, 0, salt.Length);
                Buffer.BlockCopy(saltAndKey, salt.Length, key, 0, key.Length);

                // Obtain hash
                string hmac = Base64.Encode(CalcHash(key, salt));

                //
                // Check stores
                foreach (var s in _storages) {
                    apiKey = s.GetKeyByHash(hmac).Result;

                    if (apiKey != null) {
                        break;
                    }
                }
            }

            //
            // Check expiration
            if (apiKey == null || apiKey.ExpiresOn <= DateTime.UtcNow) {
                _tokenCache.Remove(token);
                return null;
            }

            //
            // Fine. Cache a valid key
            _tokenCache.Set(token, apiKey, TokenCacheExpiration);

            return apiKey;
        }

        public async Task<IEnumerable<ApiKey>> GetAllKeys() {
            List<ApiKey> result = new List<ApiKey>();

            foreach (var s in _storages) {
                result.AddRange(await s.GetAllKeys());
            }

            return result;
        }

        public ApiKey GetKey(string id) {
            if (string.IsNullOrWhiteSpace(id)) {
                return null;
            }

            foreach (var s in _storages) {
                ApiKey key = s.GetKeyById(id).Result;
                if (key != null) {
                    return key;
                }
            }

            return null;
        }

        public async Task SaveKey(ApiKey key) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            if (string.IsNullOrEmpty(key.Id)) {
                throw new ArgumentNullException(nameof(key.Id));
            }

            key.LastModified = DateTime.UtcNow;

            foreach (var s in _storages) {
                await s.SaveKey(key);
            }
        }

        public async Task DeleteKey(ApiKey key) {
            if (key == null) {
                return;
            }

            key.LastModified = DateTime.UtcNow;

            bool resetCache = false;

            foreach (var s in _storages) {
                if (await s.RemoveKey(key)) {
                    resetCache = true;
                }
            }

            if (resetCache) {
                ResetCache();
            }
        }


        private static byte[] GenerateRandom(int bytesLen) {
            byte[] bytes = new byte[bytesLen];

            using (var rng = RandomNumberGenerator.Create()) {
                rng.GetBytes(bytes);
            }

            return bytes;
        }

        private byte[] CalcHash(byte[] key, byte[] salt) {
            //
            //
            using (var rfc2898DeriveBytes = new Rfc2898DeriveBytes(key, salt, 1000)) {
                return rfc2898DeriveBytes.GetBytes(_options.HashSize);
            }
        }

        private static string GenerateId() {
            return Base64.Encode(GenerateRandom(16));
        }

        private void ResetCache() {
            _tokenCache = new MemoryCache(new MemoryCacheOptions() {
                ExpirationScanFrequency = TokenCacheExpiration
            });
        }
    }
}
