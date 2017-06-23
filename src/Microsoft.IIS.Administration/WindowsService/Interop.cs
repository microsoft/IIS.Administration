// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WindowsService {
    using System;
    using System.Runtime.InteropServices;


    static class Interop {
        private const string SERVICE_CORE_API_SET = "api-ms-win-service-core-l1-1-0";

        public const int SERVICE_TYPE_WIN32_OWN_PROCESS = 0x00000010;
        public const int SERVICE_TYPE_WIN32_SHARE_PROCESS = 0x00000020;
        public const int SERVICE_TYPE_WIN32 = SERVICE_TYPE_WIN32_OWN_PROCESS | SERVICE_TYPE_WIN32_SHARE_PROCESS;

        public const int SERVICE_CONTROL_STOP = 0x00000001;
        public const int SERVICE_ACCEPT_STOP = 0x00000001;

        public const int SERVICE_STOPPED = 0x00000001;
        public const int SERVICE_START_PENDING = 0x00000002;
        public const int SERVICE_STOP_PENDING = 0x00000003;
        public const int SERVICE_RUNNING = 0x00000004;
        public const int SERVICE_CONTINUE_PENDING = 0x00000005;
        public const int SERVICE_PAUSE_PENDING = 0x00000006;
        public const int SERVICE_PAUSED = 0x00000007;


        [StructLayout(LayoutKind.Sequential)]
        public struct SERVICE_STATUS {
            public int serviceType;
            public int currentState;
            public int controlsAccepted;
            public int win32ExitCode;
            public int serviceSpecificExitCode;
            public int checkPoint;
            public int waitHint;
        }

        [StructLayout(LayoutKind.Sequential)]
        public class SERVICE_TABLE_ENTRY {
            public IntPtr name;
            public Delegate callback;
        }

        [DllImport(SERVICE_CORE_API_SET, CharSet = CharSet.Unicode, SetLastError = true)]
        public extern static IntPtr RegisterServiceCtrlHandlerExW(string serviceName, Delegate callback, IntPtr userData);

        [DllImport(SERVICE_CORE_API_SET, CharSet = CharSet.Unicode, SetLastError = true)]
        public extern static bool SetServiceStatus(IntPtr serviceHandle, ref SERVICE_STATUS status);

        [DllImport(SERVICE_CORE_API_SET, CharSet = CharSet.Unicode, SetLastError = true)]
        public extern static bool StartServiceCtrlDispatcherW(IntPtr entry);
    }
}