// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    static class HResults
    {
        public const int FileInUse = unchecked((int)0x80070020);
    }

    static class Win32Errors
    {
        public const int FileNotFound = 0x2;
        public const int PathNotFound = 0x3;
    }
}
