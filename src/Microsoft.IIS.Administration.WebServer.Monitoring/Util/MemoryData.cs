// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



namespace Microsoft.IIS.Administration.WebServer.Monitoring
{
    class MemoryData
    {
        private static MEMORYSTATUSEX _memoryStatus = null;

        public static long TotalInstalledMemory {
            get {
                if (_memoryStatus == null) {
                    _memoryStatus = new MEMORYSTATUSEX();
                    NativeMethods.GlobalMemoryStatusEx(_memoryStatus);
                }

                return (long)_memoryStatus.ullTotalPhys;
            }
        }
    }
}
