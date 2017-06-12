// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Core;
    using System;

    public class Defines
    {
        private const string ENDPOINT = "url-rewrite";
        private const string SERVER_VARIABLES_ENDPOINT = "server-variables";
        private const string INBOUND_RULES_SECTION_ENDPOINT = "inbound-rules";
        private const string INBOUND_RULES_ENDPOINT = "entries";

        // Feature resource
        public const string UrlRewriteName = "Microsoft.WebServer.UrlRewrite";
        public static ResDef Resource = new ResDef("url_rewrite", new Guid("BBB27846-37A0-4651-94DC-04A01B53C6B5"), ENDPOINT);
        public static readonly string PATH = $"{WebServer.Defines.PATH}/{ENDPOINT}";
        public const string IDENTIFIER = "url_rewrite.id";

        // Server Variables
        public const string ServerVariablesName = "Microsoft.WebServer.UrlRewrite.AllowedVariables";
        public static ResDef ServerVariablesResource = new ResDef("allowed_server_variables", new Guid("C8093ECD-B304-44B8-B1C5-61395FE26578"), SERVER_VARIABLES_ENDPOINT);
        public static readonly string SERVER_VARIABLES_PATH = $"{PATH}/{SERVER_VARIABLES_ENDPOINT}";
        public const string SERVER_VARIABLE_IDENTIFIER = "allowed_server_variable.id";

        // Inbound Rules Section
        public const string InboundRulesSectionName = "Microsoft.WebServer.UrlRewrite.InboundRules";
        public static ResDef InboundRulesSectionResource = new ResDef("inbound_rules", new Guid("DA8765E5-42BA-4282-9D06-016191EAE4A5"), INBOUND_RULES_SECTION_ENDPOINT);
        public static readonly string INBOUND_RULES_SECTION_PATH = $"{PATH}/{INBOUND_RULES_SECTION_ENDPOINT}";
        public const string INBOUND_RULES_SECTION_IDENTIFIER = "inbound_rules.id";

        public const string InboundRulesName = "Microsoft.WebServer.UrlRewrite.InboundRules.Entries";
        public const string InboundRuleName = "Microsoft.WebServer.UrlRewrite.InboundRules.Entry";
        public static ResDef InboundRulesResource = new ResDef("entries", new Guid("F78AD889-464B-407D-926F-F52B7CAF7595"), INBOUND_RULES_ENDPOINT);
        public static readonly string INBOUND_RULES_PATH = $"{INBOUND_RULES_SECTION_PATH}/{INBOUND_RULES_ENDPOINT}";
        public const string INBOUND_RULE_IDENTIFIER = "inbound_rule.id";
    }
}

