// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRequestTracing
{
    using Core;
    using System;

    public class Defines
    {
        private const string ENDPOINT = "http-request-tracing";
        private const string PROVIDERS_ENDPOINT = "providers";
        private const string RULES_ENDPOINT = "rules";

        public const string HttpRequestTracingName = "Microsoft.WebServer.HttpRequestTracing";
        public static readonly string PATH = $"{WebServer.Defines.PATH}/{ENDPOINT}";
        public static readonly ResDef Resource = new ResDef("request_tracing", new Guid("C6FA5E19-26F6-4332-838A-17E1353ACF9C"), ENDPOINT);
        public const string IDENTIFIER = "http_request_tracing.id";

        public const string ProvidersName = "Microsoft.WebServer.HttpRequestTracing.Providers";
        public const string ProviderName = "Microsoft.WebServer.HttpRequestTracing.Provider";
        public static readonly string PROVIDERS_PATH = $"{PATH}/{PROVIDERS_ENDPOINT}";
        public static readonly ResDef ProvidersResource = new ResDef("providers", new Guid("6C47D80C-9747-459F-8311-67ABBE630E67"), PROVIDERS_ENDPOINT);
        public const string PROVIDERS_IDENTIFIER = "provider.id";

        public const string RulesName = "Microsoft.WebServer.HttpRequestTracing.Rules";
        public const string RuleName = "Microsoft.WebServer.HttpRequestTracing.Rule";
        public static readonly string RULES_PATH = $"{PATH}/{RULES_ENDPOINT}";
        public static readonly ResDef RulesResource = new ResDef("rules", new Guid("FE6059CD-DF4D-47E9-AAB3-5C380874E323"), RULES_ENDPOINT);
        public const string RULES_IDENTIFIER = "rule.id";
    }
}
