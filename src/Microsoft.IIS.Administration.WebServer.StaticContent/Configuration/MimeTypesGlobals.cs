// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.StaticContent
{

    public static class MimeTypesGlobals {

        public const string StaticContentSectionName = "system.webServer/staticContent";

        public const int MimeMapCollection = 1;
        public const int FileExtension = 2;
        public const int MimeType = 3;
        public const int EntryType = 4;
        public const int ContentType = 5;

        // General
        public const int ReadOnly = 6;

        public enum HttpCacheControlMode
        {
            NoControl = 0,    // No client cache header is sent back ==> Not Enabled
            DisableCache = 1,    // Tell client to not cache  ==> Immediately
            UseMaxAge = 2,    // Tell client to cache using relative timeout ==> On
            UseExpires = 3     // Tell client to cache using absolute timeout ==> After
        }
    }
}
