// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.AccessManagement {
    using Core;
    using System;

    public class Defines
    {
        public const string AccessTokenName = "Microsoft.WebServer.AccessToken";
        public const string ApiKeysName = "Microsoft.WebServer.ApiKeys";
        public const string ApiKeyName = "Microsoft.WebServer.ApiKey";

        //
        // /api/access-token
        private const string MYTOKEN_ENDPOINT = "access-token";
        public static readonly string MYTOKEN_PATH = $"{Globals.API_PATH}/{MYTOKEN_ENDPOINT}";
        public static readonly ResDef MyTokenResource = new ResDef("access_token", new Guid("5677562C-F938-4D81-A706-890FB43FB0C0"), MYTOKEN_ENDPOINT);


        //
        // /security/api-keys
        private const string APIKEYS_ENDPOINT = "api-keys";
        public static readonly string APIKEYS_PATH = $"{Globals.SECURITY_PATH}/{APIKEYS_ENDPOINT}";
        public static readonly ResDef ApiKeysResource = new ResDef("api_keys", new Guid("2AECA7E1-581B-419D-95F0-320FB34B908F"), APIKEYS_ENDPOINT);


        //
        // /security/access-tokens
        private const string ACCESSTOKENS_ENDPOINT = "access-tokens";
        public static readonly string ACCESSTOKENS_PATH = $"{Globals.SECURITY_PATH}/{ACCESSTOKENS_ENDPOINT}";
        public static readonly ResDef AccessTokensResource = new ResDef("access_tokens", new Guid("FAB2EA1E-49CE-49D7-868B-E93E8BE0B538"), ACCESSTOKENS_ENDPOINT);
    }
}
