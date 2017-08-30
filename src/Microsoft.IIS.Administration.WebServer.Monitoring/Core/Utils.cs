// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Monitoring
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using System.Text;

    class Utils
    {
        public static List<string> ExpandCounterPath(string searchPattern, PdhExpansionFlags flags)
        {
            long cchPathListLength = 0;
            IntPtr mszExpandedPathList = IntPtr.Zero;
            List<string> strings = null;

            uint result = Pdh.PdhExpandWildCardPathA(null, searchPattern, mszExpandedPathList, ref cchPathListLength, flags);

            if (result == Pdh.PDH_CSTATUS_NO_OBJECT || result == Pdh.PDH_CSTATUS_NO_INSTANCE) {
                return new List<string>();
            }

            if (result != Pdh.PDH_MORE_DATA) {
                throw new Win32Exception((int)result);
            }

            mszExpandedPathList = Marshal.AllocHGlobal((int)cchPathListLength);

            try {
                result = Pdh.PdhExpandWildCardPathA(null, searchPattern, mszExpandedPathList, ref cchPathListLength, flags);

                if (result != 0) {
                    throw new Win32Exception((int)result);
                }

                byte[] buffer = new byte[cchPathListLength];
                Marshal.Copy(mszExpandedPathList, buffer, 0, buffer.Length);

                strings = new List<string>();
                int start = 0;
                int end = 0;

                do {

                    do {
                        end++;
                    }
                    while (buffer[end] != 0);

                    strings.Add(Encoding.ASCII.GetString(buffer, start, end - start));
                    start = end;

                } while (buffer[end + 1] != 0);

            }
            finally {
                Marshal.FreeHGlobal(mszExpandedPathList);
            }

            //
            // Syntax of a counter path:
            // \\Computer\PerfObject(ParentInstance/ObjectInstance#InstanceIndex)\Counter

            return strings;
        }
    }
}
