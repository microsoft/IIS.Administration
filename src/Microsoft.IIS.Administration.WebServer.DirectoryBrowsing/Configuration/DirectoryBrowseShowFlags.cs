// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.DirectoryBrowsing
{
using System;

    [Flags]
    public enum DirectoryBrowseShowFlags {

        None = 0,
    
        Date = 2,
        
        Time = 4,
        
        Size = 8,
        
        Extension = 16,
        
        LongDate = 32,
    }
}
