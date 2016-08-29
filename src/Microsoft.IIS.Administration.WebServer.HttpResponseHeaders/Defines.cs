// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpResponseHeaders
{
    using Core;
    using System;

    public class Defines
    {
        private const string ENDPOINT = "http-response-headers";
        private const string CUSTOM_HEADERS_ENDPOINT = "custom-headers";
        private const string REDIRECT_HEADERS_ENDPOINT = "redirect-headers";

        // Top level resource for plugin
        public const string ResponseHeadersName = "Microsoft.WebServer.ResponseHeaders";
        public static readonly ResDef Resource = new ResDef("response_headers", new Guid("FF1F3FDD-7EF6-4DED-96E9-B94C34B3CE76"), ENDPOINT);
        public static readonly string PATH = $"{WebServer.Defines.PATH}/{ENDPOINT}";
        public const string IDENTIFIER = "http_response_headers.id";

        // Custom Headers
        public const string CustomHeadersName = "Microsoft.WebServer.ResponseHeaders.CustomHeaders";
        public const string CustomHeaderName = "Microsoft.WebServer.ResponseHeaders.CustomHeader";
        public static readonly ResDef CustomHeadersResource = new ResDef("custom_headers", new Guid("2F9AE6A0-3A99-452B-9D1B-1517BAB144D4"), CUSTOM_HEADERS_ENDPOINT);
        public static readonly string CUSTOM_HEADERS_PATH = $"{PATH}/{CUSTOM_HEADERS_ENDPOINT}";
        public const string CUSTOM_HEADERS_IDENTIFIER = "cust_header.id";

        // Redirect Headers
        public const string RedirectHeadersName = "Microsoft.WebServer.ResponseHeaders.RedirectHeaders";
        public const string RedirectHeaderName = "Microsoft.WebServer.ResponseHeaders.RedirectHeader";
        public static readonly ResDef RedirectHeadersResource = new ResDef("redirect_headers", new Guid("65706BF8-2488-40D4-B106-A9CC186A6E79"), REDIRECT_HEADERS_ENDPOINT);
        public static readonly string REDIRECT_HEADERS_PATH = $"{PATH}/{REDIRECT_HEADERS_ENDPOINT}";
        public const string REDIRECT_HEADERS_IDENTIFIER = "red_header.id";
    }
}
