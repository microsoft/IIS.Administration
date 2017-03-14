// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRedirect
{
    using Core;
    using System;

    public class Defines
    {
        private const string ENDPOINT = "http-redirect";
        
        public const string HttpRedirectName = "Microsoft.WebServer.HttpRedirect";
        public static readonly ResDef Resource = new ResDef("http_redirect", new Guid("589B963D-CF14-45C0-B02D-2D71651DA56F"), ENDPOINT);
        public static readonly string PATH = $"{WebServer.Defines.PATH}/{ENDPOINT}";
        public const string IDENTIFIER = "http_redirect.id";
    }
}
