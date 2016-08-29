// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Handlers
{

    public enum HandlerRequiredAccess {
        
        None = 0,
        
        Read = 1,
        
        Write = 2,
        
        Script = 3,
        
        Execute = 4,
    }
}
