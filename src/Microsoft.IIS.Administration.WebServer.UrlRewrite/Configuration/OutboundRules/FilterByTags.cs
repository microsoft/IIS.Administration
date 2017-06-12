// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using System;

    [Flags()]
    enum FilterByTags {

        None = 0,

        A = 1,

        Area = 2,

        Base = 4,

        Form = 8,

        Frame = 16,

        Head = 32,

        IFrame = 64,

        Img = 128,

        Input = 256,

        Link = 512,

        Script = 1024,

        CustomTags = 32768,
    }
}

