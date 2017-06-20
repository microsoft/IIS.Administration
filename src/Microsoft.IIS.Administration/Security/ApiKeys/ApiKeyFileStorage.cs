// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Security {
    using Microsoft.IIS.Administration.Core.Security;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using System;
    using System.IO;
    using Microsoft.IIS.Administration.Core.Utils;
    using System.Security.Cryptography;
    using Newtonsoft.Json;
    using System.Linq;


    /* 
        JSON file format:
     
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
    class ApiKeyFileStorage : IApiKeyStorage {
        private string _filePath;
        private SemaphoreSlim _lock = new SemaphoreSlim(1); // All writes are sequential
        private IDictionary<string, ApiKey> _keys; // Read-only


        public ApiKeyFileStorage(string filePath) {
            if (string.IsNullOrEmpty(filePath)) {
                throw new ArgumentNullException(nameof(filePath));
            }

            _filePath = filePath;
        }

        public async Task<IEnumerable<ApiKey>> GetAllKeys() {
            await EnsureInit();

            return _keys.Values;
        }

        public async Task<ApiKey> GetKeyByHash(string hash) {
            await EnsureInit();

            ApiKey apiKey = null;
            if (_keys.TryGetValue(hash, out apiKey)) {
                return apiKey;
            }

            return null;
        }


        public async Task<ApiKey> GetKeyById(string id) {
            await EnsureInit();

            return _keys.Values.Where(k => k.Id == id).FirstOrDefault();
        }

        public async Task SaveKey(ApiKey key) {
            //
            // Sequential access
            await _lock.WaitAsync();

            try {
                // Load a fresh copy
                var keys = await LoadFile();

                // Remove an existing key
                var existing = _keys.Where(kv => kv.Value.Id == key.Id).FirstOrDefault();

                if (existing.Key != null) {
                    keys.Remove(existing.Key);
                }

                // Add
                keys.Add(key.TokenHash, key);

                // Save
                await UpdateFile(keys.Values);

                // Update the cache
                Interlocked.Exchange(ref _keys, keys); // To avoid volatile
            }
            finally {
                _lock.Release();
            }
        }

        public async Task<bool> RemoveKey(ApiKey key) {
            if (await GetKeyByHash(key.TokenHash) == null) {
                return false;
            }

            //
            // Sequential access
            await _lock.WaitAsync();

            try {
                var keys = await LoadFile();

                if (keys.Remove(key.TokenHash)) {

                    await UpdateFile(keys.Values);

                    // Update the cache
                    Interlocked.Exchange(ref _keys, keys); // To avoid volatile
                }
            }
            finally {
                _lock.Release();
            }

            return true;
        }


        private async Task EnsureInit() {
            if (_keys != null) {
                return;
            }

            //
            // Load keys from file
            await _lock.WaitAsync();

            try {
                _keys = await LoadFile();
            }
            finally {
                _lock.Release();
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

            //
            // Write into temp file to avoid corruption
            //

            //
            // Define unique temp name
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

        private static byte[] GenerateRandom(int bytesLen) {
            byte[] bytes = new byte[bytesLen];

            using (var rng = RandomNumberGenerator.Create()) {
                rng.GetBytes(bytes);
            }

            return bytes;
        }

        private static string GenerateId() {
            return Base64.Encode(GenerateRandom(16));
        }
    }
}
