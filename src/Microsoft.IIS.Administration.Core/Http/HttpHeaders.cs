// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core.Http {

    public static class HeaderNames {

        public const string AcceptPatch = "Accept-Patch";
        public const string CorsPrefix = "Access-Control";
        public const string ContentLength = "Content-Length";
        public const string ContentType = "Content-Type";
        public const string Total_Count = "X-Total-Count";
        public const string Access_Token = "Access-Token";
        public const string X_Forwarded_Proto = "X-Forwarded-Proto";
        public const string XSRF_TOKEN = "XSRF-TOKEN";
        public const string Origin = "Origin";
    }

    public static class HeaderValues
    {
        public const string FormEncoded = "application/x-www-form-urlencoded";
        public const string Hal = "application/hal+json";
    }
}
