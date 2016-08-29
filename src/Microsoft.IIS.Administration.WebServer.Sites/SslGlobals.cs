// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Sites
{
    public static class SslGlobals {

        public const string AccessModesSectionName = "system.webServer/security/access";

        public const int HttpAccessSslFlags = 0;

        public const int HasHttpsBinding = 1;

        public const int ReadOnly = 2;

    }
}
