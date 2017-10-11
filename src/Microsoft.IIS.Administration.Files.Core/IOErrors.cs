// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    public static class HResults
    {
        public const int FileInUse = unchecked((int)0x80070020);
        public const int FileNotFound = unchecked((int)0x80070003);
        public const int PathNotFound = unchecked((int)0x80070035);
    }

    static class Win32Errors
    {
        public const int FileNotFound = 0x2;
        public const int PathNotFound = 0x3;
        public const int AccessDenied = 0x5;
        public const int FileInUse = 0x20;
        public const int FileCannotBeAccessed = 0x780;
    }
}
