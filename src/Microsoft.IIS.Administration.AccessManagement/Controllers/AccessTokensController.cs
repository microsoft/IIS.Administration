// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.AccessManagement {
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using System.Web.Http;
    using AspNetCore.Antiforgery;
    using AspNetCore.Authorization;
    using AspNetCore.Cors;
    using AspNetCore.Mvc;
    using Core;
    using Core.Security;
    using Core.Utils;
    using Extensions.Options;


    /// <summary>
    /// AccessTokensController: 
    /// 
    //  CORs MUST be explicitly disabled
    //  AntiForgery MUST be applied
    /// </summary>
    [Authorize(Policy = "ApiKeys")]
    [DisableCors]
    public class AccessTokensController : ApiController {
        IApiKeyProvider _keyProvider;

        public AccessTokensController(IApiKeyProvider keyProvider) {
            if (keyProvider == null) {
                throw new ArgumentNullException(nameof(keyProvider));
            }

            _keyProvider = keyProvider;
        }


        [HttpGet]
        public void Get() {
            SetAntiForgeryTokens();

            //
            // Nothing to do here
            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.AccessTokenName)]
        public object Get(string id) {
            SetAntiForgeryTokens();

            ApiKey key = _keyProvider.GetKey(id);

            if (key == null) {
                return NotFound();
            }

            return AccessTokenHelper.ToJsonModel(key);
        }


        [ValidateAntiForgeryToken]
        [HttpPost]
        [ResourceInfo(Name = Defines.AccessTokenName)]
        public async Task<object> Post([FromBody] dynamic model) {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            if (model.api_key == null || model.api_key.id == null) {
                throw new ApiArgumentException("api_key");
            }

            string apikeyId = DynamicHelper.Value(model.api_key.id);

            ApiKey key = _keyProvider.GetKey(apikeyId);

            if (key == null) {
                return NotFound();
            }

            // Renew the token
            string token = await _keyProvider.RenewToken(key);

            //
            // Create response
            dynamic obj = AccessTokenHelper.ToJsonModel(new ApiToken() { Token=token, Key=key });
            return Created(AccessTokenHelper.GetLocation(key.Id), obj);
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
