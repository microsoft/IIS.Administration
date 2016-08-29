// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Authorization
{
    using Core;
    using System;

    public class Defines
    {
        private const string AUTHORIZATION_ENDPOINT = "authorization";
        private const string RULES_ENDPOINT = "rules";

        public const string AuthorizationName = "Microsoft.WebServer.Authorization";
        public static readonly string AUTHORIZATION_PATH = $"{WebServer.Defines.PATH}/{AUTHORIZATION_ENDPOINT}";
        public static readonly ResDef AuthorizationResource = new ResDef("authorization", new Guid("537359F9-5923-4B26-955D-223622DCC255"), AUTHORIZATION_ENDPOINT);
        public const string AUTHORIZATION_IDENTIFIER = "authorization.id";

        // Rules
        public const string RulesName = "Microsoft.WebServer.Authorization.Rules";
        public const string RuleName = "Microsoft.WebServer.Authorization.Rule";
        public static readonly string RULES_PATH = $"{AUTHORIZATION_PATH}/{RULES_ENDPOINT}";
        internal static ResDef RulesResource = new ResDef("rules", new Guid("0D3B0692-7217-4FEA-BC5D-2C3F41E5C4F0"), RULES_ENDPOINT);
        public const string RULES_IDENTIFIER = "rule.id";
    }
}
