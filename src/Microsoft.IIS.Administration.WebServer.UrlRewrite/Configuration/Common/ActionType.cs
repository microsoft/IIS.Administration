// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    internal enum ActionType
    {
        None = 0,
        Rewrite = 1,
        Redirect = 2,
        CustomResponse = 3,
        AbortRequest = 4,
    }
}

