// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.AccessManagement { 
    using AspNetCore.Builder;
    using Core;
    using Core.Http;


    public class Startup : BaseModule {
        public Startup() {
        }

        public override void Start() {
            ConfigureMyToken();
            ConfigureApiKeys();
            ConfigureAccessTokens();
        }

        private void ConfigureMyToken() {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.MyTokenResource.Guid, $"{Defines.MYTOKEN_PATH}", new { controller = "MyToken" });

            Environment.Hal.ProvideLink(Defines.MyTokenResource.Guid, "self", _ => new { href = $"/{Defines.MYTOKEN_PATH}" });
            Environment.Hal.ProvideLink(Globals.ApiResource.Guid, Defines.MyTokenResource.Name, _ => new { href = $"/{Defines.MYTOKEN_PATH}" });
        }

        private void ConfigureApiKeys() {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.ApiKeysResource.Guid, $"{Defines.APIKEYS_PATH}/{{id?}}", new { controller = "ApiKeys" });

            Environment.Hal.ProvideLink(Defines.ApiKeysResource.Guid, "self", t => new { href = ApiKeyHelper.GetLocation(t.id) });
        }

        private void ConfigureAccessTokens() {
            Environment.Host.RouteBuilder.MapWebApiRoute(Defines.AccessTokensResource.Guid, $"{Defines.ACCESSTOKENS_PATH}/{{id?}}", new { controller = "AccessTokens" });

            Environment.Hal.ProvideLink(Defines.AccessTokensResource.Guid, "self", t => new { href = AccessTokenHelper.GetLocation(t.id) });
            Environment.Hal.ProvideLink(Defines.ApiKeysResource.Guid, "access_token", k => new { href = AccessTokenHelper.GetLocation(k.id) });
        }
    }
}
