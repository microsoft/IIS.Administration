// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Security {
    using Core.Security;
    using Core.Utils;
    using Extensions.Caching.Memory;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;

    public class ApiKeyProvider : IApiKeyProvider {
        private IDictionary<string, ApiKey> _keys; // Read-only
        private string _filePath;
        private ApiKeyOptions _options;
        private SemaphoreSlim _lock = new SemaphoreSlim(1); // All writes are sequential
        private MemoryCache _tokenCache;
        private static readonly TimeSpan TokenCacheExpiration = TimeSpan.FromSeconds(30);

        public ApiKeyProvider(string filePath, ApiKeyOptions options) {
            if (string.IsNullOrEmpty(filePath)) {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }

            ResetCache();

            _filePath = filePath;
            _options = options;
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

        public async Task<ApiToken> RenewToken(ApiKey key) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            ResetCache();

            //
            // Generate a new key
            ApiToken result = GenerateKey(key.Purpose);

            // Copy the existing key metadata
            result.Key.Id = key.Id;
            result.Key.CreatedOn = key.CreatedOn;
            result.Key.ExpiresOn = key.ExpiresOn;

            //
            // Sequential access
            await _lock.WaitAsync();
            try {
                // Load a fresh copy
                var keys = await LoadFile();

                // Remove an existing key
                keys.Remove(key.TokenHash);

                // Add the generated on
                keys.Add(result.Key.TokenHash, result.Key);

                // Save
                await UpdateFile(keys.Values);

                // Update the cache
                _keys = keys;
            }
            finally {
                _lock.Release();
            }

            return result;
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

                EnsureInit();

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

                _keys.TryGetValue(hmac, out apiKey);
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

        public IEnumerable<ApiKey> GetAllKeys() {
            EnsureInit();

            return _keys.Values;
        }

        public ApiKey GetKey(string id) {
            if (string.IsNullOrWhiteSpace(id)) {
                return null;
            }

            EnsureInit();

            return _keys.Values.Where(k => k.Id == id).FirstOrDefault();
        }

        public async Task SaveKey(ApiKey key) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            if (string.IsNullOrEmpty(key.Id)) {
                throw new ArgumentNullException(nameof(key.Id));
            }

            key.LastModified = DateTime.UtcNow;

            //
            // Sequential access
            await _lock.WaitAsync();
            try {
                //
                // Load a fresh copy
                var keys = await LoadFile();

                //
                // Update
                keys[key.TokenHash] = key;

                // Save
                await UpdateFile(keys.Values);

                // Update the cache
                _keys = keys;
            }
            finally {
                _lock.Release();
            }
        }

        public async Task DeleteKey(ApiKey key) {
            if (key == null) {
                return;
            }

            if (_keys.ContainsKey(key.TokenHash)) {

                ResetCache();

                //
                // Sequential access
                await _lock.WaitAsync();
                try {
                    var keys = await LoadFile();

                    if (keys.Remove(key.TokenHash)) {

                        await UpdateFile(keys.Values);

                        _keys = keys;
                    }
                }
                finally {
                    _lock.Release();
                }
            }
        }


        private void EnsureInit() {
            if (_keys != null) {
                return;
            }

            //
            // Load keys from file
            var task = Task.Run<IDictionary<string, ApiKey>>(async ()=> {
                await _lock.WaitAsync();
                try {
                    return await LoadFile();
                }
                finally {
                    _lock.Release();
                }
            });

            _keys = task.Result;
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
            using (Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(key, salt, 1000)) {
                return rfc2898DeriveBytes.GetBytes(_options.HashSize);
            }
        }

        private async Task<IDictionary<string, ApiKey>> LoadFile() {
            var result = new Dictionary<string, ApiKey>();

            dynamic obj = null;

            if (File.Exists(_filePath)) {
                using (var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read))
                using (var sr = new StreamReader(fs)) {
                    obj = JsonConvert.DeserializeObject(await sr.ReadToEndAsync());
                }
            }

            if (obj != null && obj.keys != null) {
                foreach (var k in obj.keys) {
                    ApiKey key = FromJson(k);

                    result[key.TokenHash] = key;
                }
            }

            return result;
        }


        private async Task UpdateFile(IEnumerable<ApiKey> keys) {
            if (keys == null) {
                throw new ArgumentNullException(nameof(keys));
            }

            /* 
            {
                "keys": [
                    {
                        "id": "...........",
                        "reason": "My app key",
                        "created_on": "1999-01-01T00:00:00Z",
                        "expires_on": "1999-02-01T00:00:00Z",
                        "token_hash": "....................."
                    },
                    ...
                ]
            }
            */

            //
            // Write into temp file to avoid corruption
            //

            //
            // Define temp name
            // Try in a loop in case the file name already exists
            string filePath = null;
            do {
                filePath = Path.Combine(new FileInfo(_filePath).DirectoryName, Base64.Encode(GenerateRandom(4)) + ".api-keys.json.tmp");
            }
            while (File.Exists(filePath));

            //
            // Write to file
            using (var sw = File.AppendText(filePath)) {
                FileInfo fi = new FileInfo(filePath);
                fi.Attributes = FileAttributes.Temporary;

                await sw.WriteAsync("{\r\n \"keys\": [");

                //
                // Write each key
                for (int i = 0; i < keys.Count(); ++i) {
                    var key = ToJson(keys.ElementAt(i));

                    string obj = String.Format("\r\n  {0}{1}", 
                                               JsonConvert.SerializeObject(key, Formatting.Indented).Replace("\n", "\n  "), 
                                               i < keys.Count() - 1 ? "," : "");
                    await sw.WriteAsync(obj);
                }

                await sw.WriteAsync("\r\n ]\r\n}");
                await sw.FlushAsync();
            }

            //
            // Swap the original file
            File.Delete(_filePath);
            File.Move(filePath, _filePath);
        }


        private static object ToJson(ApiKey key) {
            return new {
                id = key.Id,
                purpose = key.Purpose ?? "",
                created_on = key.CreatedOn,
                last_modified = key.LastModified,
                expires_on = (object)key.ExpiresOn ?? string.Empty,
                token_hash = key.TokenHash,
                token_type = key.TokenType
            };
        }

        private static ApiKey FromJson(dynamic key) {
            string tokenHash = DynamicHelper.Value(key.token_hash) ?? DynamicHelper.Value(key.hash);
            string tokenType = DynamicHelper.Value(key.token_type) ?? "SWT";
            DateTime createdOn = DynamicHelper.To<DateTime>(key.created_on) ?? DateTime.UtcNow;

            return new ApiKey(tokenHash, tokenType) {
                Id = DynamicHelper.Value(key.id) ?? GenerateId(),
                Purpose = DynamicHelper.Value(key.purpose) ?? string.Empty,
                CreatedOn = createdOn,
                ExpiresOn = DynamicHelper.To<DateTime>(key.expires_on),
                LastModified = DynamicHelper.To<DateTime>(key.last_modified) ?? createdOn
            };
        }

        private static string GenerateId() {
            return Base64.Encode(GenerateRandom(16));
        }

        private void ResetCache()
        {
            _tokenCache = new MemoryCache(new MemoryCacheOptions() {
                ExpirationScanFrequency = TokenCacheExpiration
            });
        }
    }
}
