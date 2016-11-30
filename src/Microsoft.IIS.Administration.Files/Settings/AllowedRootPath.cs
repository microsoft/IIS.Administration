// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    public class AllowedRoot
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public bool Read_Only { get; set; } = true;
    }
}
