// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Monitoring
{
    using Microsoft.IIS.Administration.Monitoring;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading.Tasks;

    class ProcessUtil
    {
        public static IEnumerable<int> GetWebserverProcessIds()
        {
            List<int> ids = new List<int>();
            Dictionary<int, int> map = GetProcessMap();

            foreach (var workerProcess in Process.GetProcessesByName("W3WP")) {

                ids.Add(workerProcess.Id);

                foreach (var kvp in map) {

                    if (kvp.Value == workerProcess.Id) {
                        ids.Add(kvp.Key);
                    }
                }
            }

            return ids;
        }

        //
        // Process counters instance names are not equivalent to their process IDs therefore a map must be generated to distinguish them
        // key: process id
        // value: process counter instance name
        public static async Task<Dictionary<int, string>> GetProcessCounterMap(ICounterProvider provider, string processName)
        {
            var map = new Dictionary<int, string>();

            var instances = (await provider.GetInstances(ProcessCounterNames.Category)).Where(instance => instance.StartsWith(processName, StringComparison.OrdinalIgnoreCase));

            List<IPerfCounter> counters = new List<IPerfCounter>();

            try {
                foreach (string instance in instances) {
                    counters.AddRange(await provider.GetCounters(ProcessCounterNames.Category, instance, ProcessCounterNames.CounterNames));
                }
            }
            catch (MissingCountersException) {
                //
                // map will remain empty
            }

            foreach (IPerfCounter counter in counters) {
                if (counter.Name.Equals(ProcessCounterNames.ProcessId)) {

                    //
                    // process id fits in int
                    map[(int)counter.Value] = counter.InstanceName;
                }
            }

            return map;
        }

        private static IEnumerable<int> GetAppPoolProcessIds()
        {
            List<int> ids = new List<int>();
            Dictionary<int, int> map = GetProcessMap();

            foreach (var workerProcess in Process.GetProcessesByName("W3WP")) {

                ids.Add(workerProcess.Id);

                foreach (var kvp in map) {

                    if (kvp.Value == workerProcess.Id) {
                        ids.Add(kvp.Key);
                    }
                }
            }

            return ids;
        }

        //
        // Key: processId
        // value: parentProcessId
        private static Dictionary<int, int> GetProcessMap()
        {
            var map = new Dictionary<int, int>();
            IntPtr hSnapshot = IntPtr.Zero;

            try {
                PROCESSENTRY32 procEntry = default(PROCESSENTRY32);
                procEntry.dwSize = (uint) Marshal.SizeOf<PROCESSENTRY32>();

                hSnapshot = NativeMethods.CreateToolhelp32Snapshot((uint)SnapshotFlags.Process, 0);

                if (NativeMethods.Process32First(hSnapshot, ref procEntry)) {

                    do {
                        map.Add((int)procEntry.th32ProcessID, (int)procEntry.th32ParentProcessID);

                    } while (NativeMethods.Process32Next(hSnapshot, ref procEntry));
                }
                else {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            finally {
                NativeMethods.CloseHandle(hSnapshot);
            }

            return map;
        }
    }

    [Flags]
    enum SnapshotFlags : uint
    {
        HeapList = 0x00000001,
        Process = 0x00000002,
        Thread = 0x00000004,
        Module = 0x00000008,
        Module32 = 0x00000010,
        Inherit = 0x80000000,
        All = 0x0000001F,
        NoHeaps = 0x40000000
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct PROCESSENTRY32
    {
        const int MAX_PATH = 260;
        internal UInt32 dwSize;
        internal UInt32 cntUsage;
        internal UInt32 th32ProcessID;
        internal IntPtr th32DefaultHeapID;
        internal UInt32 th32ModuleID;
        internal UInt32 cntThreads;
        internal UInt32 th32ParentProcessID;
        internal Int32 pcPriClassBase;
        internal UInt32 dwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
        internal string szExeFile;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    class MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
        public MEMORYSTATUSEX()
        {
            this.dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>();
        }
    }

    class NativeMethods
    {
        private const string ProcessLib = "kernel32";
        private const string SysInfoApiSet = "api-ms-win-core-sysinfo-l1-1-0.dll";
        public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        [DllImport(SysInfoApiSet, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        [DllImport(ProcessLib, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateToolhelp32Snapshot([In]UInt32 dwFlags, [In]UInt32 th32ProcessID);

        [DllImport(ProcessLib, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool Process32First([In]IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport(ProcessLib, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool Process32Next([In]IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

        [DllImport(ProcessLib, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle([In] IntPtr hObject);
    }
}
