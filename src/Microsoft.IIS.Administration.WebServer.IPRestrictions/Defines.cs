// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.IPRestrictions
{
    using Core;
    using System;

    public class Defines
    {
        private const string ENDPOINT = "ip-restrictions";
        private const string RULES_ENDPOINT = "entries";

        // Top level resource for plugin
        public const string IpRestrictionsName = "Microsoft.WebServer.IpRestrictions";
        public static readonly ResDef Resource = new ResDef("ip_restrictions", new Guid("09B3DCE1-05D1-42DB-9DE7-27B2EC276483"), ENDPOINT);
        public static readonly string PATH = $"{WebServer.Defines.PATH}/{ENDPOINT}";
        public const string IDENTIFIER = "ip_restriction.id";

        // Rule
        public const string EntriesName = "Microsoft.WebServer.IpRestrictions.Entries";
        public const string EntryName = "Microsoft.WebServer.IpRestrictions.Entry";
        public static readonly ResDef RulesResource = new ResDef("entries", new Guid("C9D0C8E3-0AC2-4F67-8150-3C8E01E775F8"), RULES_ENDPOINT);
        public static readonly string RULES_PATH = $"{PATH}/{RULES_ENDPOINT}";
        public const string RULES_IDENTIFIER = "rule.id";
    }
}
