// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    // Keep public for resolution of enums from 'dynamic' types in helper classes i.e. DynamicHelper
    public enum MatchType {
        
        Pattern = 0,
        
        IsFile = 1,
        
        IsDirectory = 2,
    }
}

