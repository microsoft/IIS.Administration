// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRedirect
{
    enum RedirectHttpResponseStatus
    {
        Permanent = 301,
        Found = 302,
        Temporary = 307,
        PermRedirect = 308
    }
}
