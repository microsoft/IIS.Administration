// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using System;

    public interface IRedirect
    {
        string Name { get; set; }
        Func<string> To { get; set; }
        bool Permanent { get; set; }
    }
}
