// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Handlers
{
    using System;

    [Flags]
    public enum HandlerAccessPolicy {
        None = 0x00000000,
        Read = 0x00000001,
        Write = 0x00000002,
        Execute = 0x00000004,
        Source = 0x00000010,
        Script = 0x00000200,
        NoRemoteWrite = 0x00000400,
        NoRemoteRead = 0x00001000,
        NoRemoteExecute = 0x00002000,
        NoRemoteScript = 0x00004000,
    }
}
