// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering
{
    using Core;
    using System;

    public class Defines
    {
        private const string ENDPOINT = "http-request-filtering";
        private const string FILE_NAME_EXTENSIONS_ENDPOINT = "file-extensions";
        private const string RULES_ENDPOINT = "rules";
        private const string HEADER_LIMITS_ENDPOINT = "http-headers";
        private const string HIDDEN_SEGMENTS_ENDPOINT = "hidden-segments";
        private const string QUERY_STRING_ENDPOINT = "query-strings";
        private const string URLS_ENDPOINT = "urls";
        private const string VERBS_ENDPOINT = "http-methods";

        // Feature resource
        public const string RequestFilteringName = "Microsoft.WebServer.RequestFiltering";
        public static ResDef Resource = new ResDef("request_filtering", new Guid("6F97DDAF-18E3-4271-B8ED-516137072CED"), ENDPOINT);
        public static readonly string PATH = $"{WebServer.Defines.PATH}/{ENDPOINT}";
        public const string IDENTIFIER = "request_filtering.id";

        // File name extensions
        public const string FileExtensionsName = "Microsoft.WebServer.RequestFiltering.FileExtensions";
        public const string FileExtensionName = "Microsoft.WebServer.RequestFiltering.FileExtension";
        public static ResDef FileExtensionsResource = new ResDef("file_extensions", new Guid("93793871-A802-4092-BEA7-A820FEC21DD1"), FILE_NAME_EXTENSIONS_ENDPOINT);
        public static readonly string FILE_NAME_EXTENSIONS_PATH = $"{PATH}/{FILE_NAME_EXTENSIONS_ENDPOINT}";
        public const string FILE_NAME_EXTENSIONS_IDENTIFIER = "ext.id";

        // Filtering rules
        public const string FilteringRulesName = "Microsoft.WebServer.RequestFiltering.Rules";
        public const string FilteringRuleName = "Microsoft.WebServer.RequestFiltering.Rule";
        public static ResDef RulesResource = new ResDef("rules", new Guid("8F8C7A79-1618-4065-8BFF-ACDFAC31C457"), RULES_ENDPOINT);
        public static readonly string RULES_PATH = $"{PATH}/{RULES_ENDPOINT}";
        public const string RULES_IDENTIFIER = "rule.id";

        // Header limits
        public const string HeaderLimitsName = "Microsoft.WebServer.RequestFiltering.HeaderLimits";
        public const string HeaderLimitName = "Microsoft.WebServer.RequestFiltering.HeaderLimit";
        public static ResDef HeaderLimitsResource = new ResDef("header_limits", new Guid("1263774B-5735-4870-8D53-79AE98C061A1"), HEADER_LIMITS_ENDPOINT);
        public static readonly string HEADER_LIMITS_PATH = $"{PATH}/{HEADER_LIMITS_ENDPOINT}";
        public const string HEADER_LIMITS_IDENTIFIER = "header.id";

        // Hidden segments
        public const string HiddenSegmentsName = "Microsoft.WebServer.RequestFiltering.HiddenSegments";
        public const string HiddenSegmentName = "Microsoft.WebServer.RequestFiltering.HiddenSegment";
        public static ResDef HiddenSegmentsResource = new ResDef("hidden_segments", new Guid("8EAC7890-0475-4E4B-9131-687A0DAC4B67"), HIDDEN_SEGMENTS_ENDPOINT);
        public static readonly string HIDDEN_SEGMENTS_PATH = $"{PATH}/{HIDDEN_SEGMENTS_ENDPOINT}";
        public const string HIDDEN_SEGMENTS_IDENTIFIER = "segment.id";

        // Query strings
        public const string QueryStringsName = "Microsoft.WebServer.RequestFiltering.QueryStrings";
        public const string QueryStringName = "Microsoft.WebServer.RequestFiltering.QueryString";
        public static ResDef QueryStringResource = new ResDef("query_strings", new Guid("5B76A81F-0E4D-47C6-B80C-842B0D3E0AFA"), QUERY_STRING_ENDPOINT);
        public static readonly string QUERY_STRING_PATH = $"{PATH}/{QUERY_STRING_ENDPOINT}";
        public const string QUERY_STRING_IDENTIFIER = "query.id";

        // Urls
        public const string UrlsName = "Microsoft.WebServer.RequestFiltering.Urls";
        public const string UrlName = "Microsoft.WebServer.RequestFiltering.Url";
        public static ResDef UrlsResource = new ResDef("urls", new Guid("0AFEBCC6-917B-4056-BEDD-D57246AC7877"), URLS_ENDPOINT);
        public static readonly string URLS_PATH = $"{PATH}/{URLS_ENDPOINT}";
        public const string URLS_IDENTIFIER = "url.id";
    }
}
