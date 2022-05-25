// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.AccessManagement {
    using System;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Core.Security;


    [Route("api/access-token")]
    public class MyTokenController : ApiBaseController {
        IApiKeyProvider _keyProvider;

        public MyTokenController(IApiKeyProvider keyProvider) {
            if (keyProvider == null) {
                throw new ArgumentNullException(nameof(keyProvider));
            }

            _keyProvider = keyProvider;
        }


        [HttpGet]
        [ResourceInfo(Name = Defines.AccessTokenName)]
        public object Get() {
            ApiKey key = GetCurrentApiKey();

            return key == null ? NotFound() : (object)AccessTokenHelper.ToJsonModel(key);
        }


        [HttpPost]
        [ResourceInfo(Name = Defines.AccessTokenName)]
        public async Task<object> Post() {
            ApiKey key = GetCurrentApiKey();

            if (key == null) {
                return NotFound();
            }

            // Renew the key
            string token = await _keyProvider.RenewToken(key);

            return Created(RequestUri.PathAndQuery,
                           AccessTokenHelper.ToJsonModel(new ApiToken() { Token=token, Key=key }));
        }



        private ApiKey GetCurrentApiKey() {
            var principal = Context.User as ClaimsPrincipal;

            if (principal == null) {
                return null;
            }

            Claim tokenClaim = principal.Claims.Where(c => c.Type == Core.Security.ClaimTypes.AccessToken).FirstOrDefault();

            return tokenClaim == null ? null : _keyProvider.FindKey(tokenClaim.Value);
        }

        //protected override string GetId() {
        //    ApiKey key = GetCurrentApiKey();

        //    return key == null ? throw new NotFoundException("Access Token") : key.Id;
        //}
    }
}
