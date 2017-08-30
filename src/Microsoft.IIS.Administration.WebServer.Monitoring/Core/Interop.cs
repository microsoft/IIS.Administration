// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Monitoring
{
    using System;
    using System.Runtime.InteropServices;

    public abstract class SafeHandleZeroOrMinusOneIsInvalid : SafeHandle
    {
        public SafeHandleZeroOrMinusOneIsInvalid(IntPtr invalidHandleValue, bool ownsHandle) : base(invalidHandleValue, ownsHandle)
        {
        }

        public SafeHandleZeroOrMinusOneIsInvalid(bool ownsHandle) : base(IntPtr.Zero, ownsHandle)
        {
        }

        public override bool IsInvalid {
            get {
                return handle == IntPtr.Zero || handle == new IntPtr(-1);
            }
        }
    }
    public class PdhLogHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public PdhLogHandle() : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return Pdh.PdhCloseLog(handle, Pdh.PDH_FLAGS_CLOSE_QUERY) == 0;
        }
    }

    public class PdhQueryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public PdhQueryHandle() : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return Pdh.PdhCloseQuery(handle) == 0;
        }
    }

    public class PdhCounterHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public PdhCounterHandle() : base(true)
        {
        }

        protected override bool ReleaseHandle()
        {
            return Pdh.PdhRemoveCounter(handle) == 0;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct PDH_FMT_COUNTERVALUE
    {
        [FieldOffset(0)]
        public uint CStatus;
        [FieldOffset(8)]
        public int longValue;
        [FieldOffset(8)]
        public double doubleValue;
        [FieldOffset(8)]
        public long longLongValue;
        [FieldOffset(8)]
        public IntPtr AnsiStringValue;
        [FieldOffset(8)]
        public IntPtr WideStringValue;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct PDH_COUNTER_INFO
    {
        public UInt32 dwLength;
        public UInt32 dwType;
        public UInt32 CVersion;
        public UInt32 CStatus;
        public Int64 lScale;
        public Int64 lDefaultScale;
    }

    [Flags()]
    public enum PdhFormat : uint
    {
        PDH_FMT_RAW = 0x00000010,
        PDH_FMT_ANSI = 0x00000020,
        PDH_FMT_UNICODE = 0x00000040,
        PDH_FMT_LONG = 0x00000100,
        PDH_FMT_DOUBLE = 0x00000200,
        PDH_FMT_LARGE = 0x00000400,
        PDH_FMT_NOSCALE = 0x00001000,
        PDH_FMT_1000 = 0x00002000,
        PDH_FMT_NODATA = 0x00004000
    }

    [Flags()]
    public enum PdhExpansionFlags
    {
        NONE = 0,
        PDH_NOEXPANDCOUNTERS  = 1,
        PDH_NOEXPANDINSTANCES  = 2
    }

    [Flags()]
    public enum PerfCounterAttributes // winperf.h
    {
        PERF_TYPE_NUMBER = 0x00000000,
        PERF_TYPE_COUNTER = 0x00000400
    }

    public class Pdh
    {
        public const UInt32 PDH_FLAGS_CLOSE_QUERY = 1;
        public const UInt32 PDH_MORE_DATA = 0x800007D2;
        public const UInt32 PDH_NO_DATA = 0x800007D5;
        public const UInt32 PDH_NO_MORE_DATA = 0xC0000BCC;
        public const UInt32 PDH_INVALID_DATA = 0xC0000BC6;
        public const UInt32 PDH_CSTATUS_INVALID_DATA = 0xC0000BBA;
        public const UInt32 PDH_CALC_NEGATIVE_VALUE = 0x800007D8;
        public const UInt32 PDH_CALC_NEGATIVE_DENOMINATOR = 0x800007D6;
        public const UInt32 PDH_CSTATUS_NO_INSTANCE = 0x800007D1;
        public const UInt32 PDH_INVALID_HANDLE = 0xC0000BBC;
        public const UInt32 PDH_ENTRY_NOT_IN_LOG_FILE = 0xC0000BCD;
        public const UInt32 PDH_CSTATUS_NO_OBJECT = 0xC0000BB8;

        [DllImport("pdh.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern UInt32 PdhOpenQueryW(
         string szDataSource,
         IntPtr dwUserData,
         out PdhQueryHandle phQuery);

        [DllImport("pdh.dll", SetLastError = true)]
        public static extern UInt32 PdhOpenQueryH(
         PdhLogHandle hDataSource,
         IntPtr dwUserData,
         out PdhQueryHandle phQuery);

        [DllImport("pdh.dll", SetLastError = true)]
        public static extern UInt32 PdhCloseLog(
            IntPtr hLog,
            long dwFlags);

        [DllImport("pdh.dll", SetLastError = true)]
        public static extern UInt32 PdhCloseQuery(
            IntPtr hQuery);

        [DllImport("pdh.dll", SetLastError = true)]
        public static extern UInt32 PdhRemoveCounter(
            IntPtr hQuery);

        [DllImport("pdh.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern UInt32 PdhAddCounterW(
            PdhQueryHandle hQuery,
            string szFullCounterPath,
            IntPtr dwUserData,
            out PdhCounterHandle phCounter);

        [DllImport("pdh.dll", SetLastError = true)]
        public static extern UInt32 PdhCollectQueryData(
            PdhQueryHandle phQuery);

        [DllImport("pdh.dll", SetLastError = true)]
        public static extern UInt32 PdhGetFormattedCounterValue(
            PdhCounterHandle phCounter,
            PdhFormat dwFormat,
            IntPtr lpdwType,
            out PDH_FMT_COUNTERVALUE pValue);

        [DllImport("pdh.dll", SetLastError = true)]
        public static extern UInt32 PdhGetRawCounterValue(
            PdhCounterHandle phCounter,
            IntPtr lpdwType,
            out PDH_FMT_COUNTERVALUE pValue);

        [DllImport("pdh.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern UInt32 PdhExpandWildCardPathA(
            string szdataSource,
            string szWildCardPath,
            IntPtr mszExpandedPathList,
            ref long pcchPathListLength,
            PdhExpansionFlags dwFlags);

        [DllImport("pdh.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern UInt32 PdhGetCounterInfo(
            PdhCounterHandle hCounter,
            bool bRetrieveExplainText,
            ref int pdwBufferSize,
            IntPtr lpBuffer);
    }
}
