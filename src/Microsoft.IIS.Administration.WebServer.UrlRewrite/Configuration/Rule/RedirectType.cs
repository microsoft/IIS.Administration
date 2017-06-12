// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    enum RedirectType {
        
        Permanent = 301,
        
        Found = 302,

        SeeOther = 303,
        
        Temporary = 307,
    }
}

