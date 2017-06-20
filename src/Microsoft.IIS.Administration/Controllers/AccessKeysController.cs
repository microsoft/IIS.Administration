// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AspNetCore.Cors;
    using AspNetCore.Mvc;
    using Core.Security;
    using Core.Utils;
    using Microsoft.AspNetCore.Authorization;


    [DisableCors]
    [Authorize(Policy ="ApiKeys")]
    public class AccessKeysController : Controller {
        private static IComparer<ApiKey> _comparer = new ApiKeyComparer();
        private IApiKeyProvider _keyProvider;


        public AccessKeysController(IApiKeyProvider keyProvider) {
            _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
        }

        [HttpGet]
        public async Task<IActionResult> Index() {
            return View("Index", new {
                Keys = await GetAllKeys(),
                NewToken = (object)null
            }.ToExpando());
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Create(string purpose, string expiration) {

            long exp = 0; // seconds

            if (!string.IsNullOrWhiteSpace(expiration)) {
                exp = long.Parse(expiration);

                if (exp < 0) {
                    throw new ArgumentOutOfRangeException(nameof(expiration));
                }
            }


            // Create a key
            ApiToken key = _keyProvider.GenerateKey(purpose);

            // Set expiration
            if (exp > 0) {
                key.Key.ExpiresOn = DateTime.UtcNow.AddSeconds(exp);
            }

            // Store the key
            await _keyProvider.SaveKey(key.Key);

            return View("Index", new {
                Keys = await GetAllKeys(),
                NewToken = new {
                    Purpose = purpose,
                    Value = key.Token
                }.ToExpando()
            }.ToExpando());
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> Delete() {

            var form = await Request.ReadFormAsync();
            ApiKey key = _keyProvider.GetKey(form["id"]);

            if (key != null) {
                await _keyProvider.DeleteKey(key);
            }

            return RedirectToAction("Index");
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public async Task<IActionResult> RefreshToken() {

            var form = await Request.ReadFormAsync();
            ApiKey key = _keyProvider.GetKey(form["id"]);

            if (key == null) {
                return NotFound();
            }

            string token = await _keyProvider.RenewToken(key);

            return View("Index", new {
                Keys = await GetAllKeys(),
                NewToken = new {
                    Purpose = key.Purpose,
                    Value = token
                }.ToExpando()
            }.ToExpando());
        }

        private async Task<IEnumerable<ApiKey>> GetAllKeys() {
            return (await _keyProvider.GetAllKeys()).OrderBy(k => k, _comparer);
        }
    }


    class ApiKeyComparer : IComparer<ApiKey> {

        public int Compare(ApiKey k1, ApiKey k2) {

            if (object.ReferenceEquals(k1, k2) || k1.TokenHash == k2.TokenHash) {
                return 0;
            }

            if (k1.ExpiresOn == k2.ExpiresOn) {
                return (k1.CreatedOn >= k2.CreatedOn) ? -1 : 1;
            }

            if (k1.ExpiresOn == null) {
                return 1;
            }

            if (k2.ExpiresOn == null) {
                return -1;
            }

            return (k1.ExpiresOn >= k2.ExpiresOn) ? 1 : -1;
        }
    }
}
