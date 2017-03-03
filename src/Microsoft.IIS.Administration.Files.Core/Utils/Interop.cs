// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using System.Runtime.InteropServices;
    using System.Text;

    static class Interop
    {
        private const string CORE_FILE_API_SET = "api-ms-win-core-file-l1-1-0";

        [DllImport(CORE_FILE_API_SET, SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.U4)]
        private static extern uint GetLongPathNameW(string lpszShortPath, StringBuilder lpszLongPath, int cchBuffer);

        [DllImport(CORE_FILE_API_SET, SetLastError = true, CharSet = CharSet.Unicode)]
        static extern uint GetShortPathNameW(string longpath, StringBuilder sb, int buffer);

        public static string GetPath(string path)
        {
            var sb = new StringBuilder(260);

            uint result = GetShortPathNameW(path, sb, sb.Capacity);

            if (result > 0) {
                result = GetLongPathNameW(sb.ToString(), sb, sb.Capacity);
            }

            if (result > sb.Capacity) {
                sb.Capacity = (int)result;
                result = GetLongPathNameW(path, sb, sb.Capacity);
            }

            return (result > 0 && result < sb.Capacity) ? sb.ToString(0, (int)result) : path;
        }
    }
}
