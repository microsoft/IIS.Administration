// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.AccessManagement {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using AspNetCore.Antiforgery;
    using AspNetCore.Authorization;
    using AspNetCore.Cors;
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Core.Security;
    using Core.Utils;
    using Extensions.Options;


    /// <summary>
    /// ApiKeysController: 
    /// 
    //  CORs MUST be explicitly disabled
    //  AntiForgery MUST be applied
    /// </summary>
    [Authorize(Policy = "ApiKeys")]
    [DisableCors]
    public class ApiKeysController : ApiEdgeController {
        IApiKeyProvider _keyProvider;

        public ApiKeysController(IApiKeyProvider keyProvider) {
            _keyProvider = keyProvider ?? throw new ArgumentNullException(nameof(keyProvider));
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.ApiKeysName)]
        public async Task<object> Get() {
            SetAntiForgeryTokens();

            IEnumerable<ApiKey> keys = await _keyProvider.GetAllKeys();

            // Set HTTP header for total count
            Context.Response.SetItemsCount(keys.Count());

            return new {
                api_keys = keys.Select(k => ApiKeyHelper.ToJsonModel(k))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.ApiKeyName)]
        public object Get(string id) {
            ApiKey key = _keyProvider.GetKey(id);

            if (key == null) {
                return NotFound();
            }

            SetAntiForgeryTokens();

            return ApiKeyHelper.ToJsonModel(key);
        }


        [ValidateAntiForgeryToken]
        [HttpPost]
        [ResourceInfo(Name = Defines.ApiKeyName)]
        public async Task<object> Post([FromBody] dynamic model) {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            if (model.expires_on == null) {
                throw new ApiArgumentException("expires_on");
            }

            string purpose = DynamicHelper.Value(model.purpose) ?? string.Empty;
            DateTime? expiresOn = DynamicHelper.Value(model.expires_on) != String.Empty ? DynamicHelper.To<DateTime>(model.expires_on) : null;

            ApiToken token = _keyProvider.GenerateKey(purpose);

            token.Key.ExpiresOn = expiresOn;

            await _keyProvider.SaveKey(token.Key);

            //
            // Create response
            dynamic key = ApiKeyHelper.ToJsonModel(token);
            return Created(ApiKeyHelper.GetLocation(key.id), key);
        }


        [ValidateAntiForgeryToken]
        [HttpPatch]
        [ResourceInfo(Name = Defines.ApiKeyName)]
        public async Task<object> Patch(string id, [FromBody] dynamic model) {
            ApiKey key = _keyProvider.GetKey(id);

            if (key == null) {
                return NotFound();
            }

            ApiKeyHelper.Update(key, model);

            await _keyProvider.SaveKey(key);

            return ApiKeyHelper.ToJsonModel(key);
        }


        [ValidateAntiForgeryToken]
        [HttpDelete]
        public async Task Delete(string id) {
            ApiKey key = _keyProvider.GetKey(id);

            if (key != null) {
                await _keyProvider.DeleteKey(key);
            }

            //
            // Success
            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }


        private AntiforgeryTokenSet SetAntiForgeryTokens() {
            IAntiforgery antiforgery = (IAntiforgery) Context.RequestServices.GetService(typeof(IAntiforgery));
            IOptions<AntiforgeryOptions> options = (IOptions<AntiforgeryOptions>)Context.RequestServices.GetService(typeof(IOptions<AntiforgeryOptions>));

            // Save cookie
            AntiforgeryTokenSet tokens = antiforgery.GetAndStoreTokens(Context);

            // Set response header
            Context.Response.Headers[options.Value.FormFieldName] = tokens.RequestToken;

            return tokens;
        }
    }
}
