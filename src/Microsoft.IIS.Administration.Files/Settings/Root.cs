// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using System.Collections.Generic;

    public class Root
    {
        public string Alias { get; set; }
        public string Path { get; set; }
        public List<string> Permissions { get; set; } = new List<string>();
    }
}
