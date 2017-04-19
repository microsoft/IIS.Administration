// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using Win32.SafeHandles;

    public class SymLink
    {
        private const string CORE_FILE_API_SET = "api-ms-win-core-file-l1-1-0";
        private const string CORE_IO_API_SET = "api-ms-win-core-io-l1-1-0";

        private const int FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
        private const int FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;
        private const uint FSCTL_GET_REPARSE_POINT = 0x000900A8;

        private const uint ERROR_PATH_NOT_REPARSE_POINT = 0x1126;
        private const uint ERROR_MORE_DATA = 0xEA;
        private const uint ERROR_INSUFFICIENT_BUFFER = 0x7A;

        private enum ReparseTagType : uint
        {
            IO_REPARSE_TAG_MOUNT_POINT = 0xA0000003,
            IO_REPARSE_TAG_HSM = 0xC0000004,
            IO_REPARSE_TAG_SIS = 0x80000007,
            IO_REPARSE_TAG_DFS = 0x8000000A,
            IO_REPARSE_TAG_SYMLINK = 0xA000000C,
            IO_REPARSE_TAG_DFSR = 0x80000012,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct REPARSE_DATA_BUFFER
        {
            public const int MAX_BUFFER_SIZE = 16 * 1024;// 16K limit
            public const int BUFFER_SIZE = 260;

            public ReparseTagType ReparseTag;
            public ushort ReparseDataLength;
            public ushort Reserved;
            public ushort SubstituteNameOffset;
            public ushort SubstituteNameLength;
            public ushort PrintNameOffset;
            public ushort PrintNameLength;
            public IntPtr PathBuffer;
        }

        [DllImport(CORE_IO_API_SET, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool DeviceIoControl(IntPtr hDevice,
                                                    uint dwIoControlCode,
                                                    IntPtr InBuffer,
                                                    int nInBufferSize,
                                                    IntPtr OutBuffer,
                                                    int nOutBufferSize,
                                                    out int pBytesReturned,
                                                    IntPtr lpOverlapped);

        [DllImport(CORE_FILE_API_SET, CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFileW([MarshalAs(UnmanagedType.LPWStr)] string filename,
                                                          [MarshalAs(UnmanagedType.U4)] FileAccess access,
                                                          [MarshalAs(UnmanagedType.U4)] FileShare share,
                                                          IntPtr securityAttributes,
                                                          [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
                                                          uint flagsAndAttributes,
                                                          IntPtr templateFile);


        [DllImport(CORE_FILE_API_SET, SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint GetFinalPathNameByHandleW(IntPtr hFile,
                                                     [MarshalAs(UnmanagedType.LPTStr)]
                                                 StringBuilder lpszFilePath,
                                                     uint cchFilePath,
                                                     uint dwFlags);


        private string _target;
        private bool _resolved;

        public SymLink(string path)
        {
            if (string.IsNullOrEmpty(path)) {
                throw new ArgumentNullException(nameof(path));
            }

            Path = path;
        }

        public string Path { get; private set; }

        public string Target {
            get {
                if (!_resolved) {
                    _target = GetTargetPath();
                    _resolved = true;
                }

                return _target;
            }
        }

        private string GetTargetPath()
        {
            IntPtr buff = IntPtr.Zero;
            int dataSize = 0;

            SafeFileHandle handle = OpenFile();

            if (handle.IsInvalid) {
                return null;
            }

            try {
                //
                // Try REPARSE data
                if (FindReparsePoint(handle, out buff, out dataSize)) {
                    REPARSE_DATA_BUFFER reparseData = Marshal.PtrToStructure<REPARSE_DATA_BUFFER>(buff);

                    if (reparseData.ReparseTag == ReparseTagType.IO_REPARSE_TAG_SYMLINK ||
                        reparseData.ReparseTag == ReparseTagType.IO_REPARSE_TAG_MOUNT_POINT) {
                        //
                        // Since the data size isn't fixed, Marshal.PtrToStructure can't return via reparseData.PathBuffer 
                        // Therefore we need to marshal it manually from the native data
                        int offset = Marshal.SizeOf<REPARSE_DATA_BUFFER>();
                        byte[] data = new byte[dataSize];

                        Marshal.Copy(new IntPtr(buff.ToInt64() + offset), data, 0, data.Length);

                        //
                        // Symlink
                        if (reparseData.ReparseTag == ReparseTagType.IO_REPARSE_TAG_SYMLINK) {
                            return Encoding.Unicode.GetString(data, reparseData.PrintNameOffset, reparseData.PrintNameLength);
                        }

                        //
                        // Junction
                        return Encoding.Unicode.GetString(data, reparseData.PrintNameOffset - Marshal.SizeOf<IntPtr>(), reparseData.PrintNameLength);
                    }
                }

                //
                // Fallback if REPARSE point can't be used
                return GetFinalPathNameByHandle(handle);
            }
            finally {
                handle.Dispose();
                Marshal.FreeHGlobal(buff);
            }
        }

        private SafeFileHandle OpenFile()
        {
            SafeFileHandle handle = CreateFileW(Path,
                                                FileAccess.Read,
                                                FileShare.ReadWrite,
                                                IntPtr.Zero,
                                                FileMode.Open,
                                                FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAG_OPEN_REPARSE_POINT,
                                                IntPtr.Zero);

            if (handle.IsInvalid) {
                int error = Marshal.GetLastWin32Error();

                //
                // Do nothing if file does not exist
                if (error == Win32Errors.FileNotFound || error == Win32Errors.PathNotFound) {
                    return handle;
                }

                //
                // Access Denied
                if (error == Win32Errors.AccessDenied || error == Win32Errors.FileCannotBeAccessed) {
                    throw new UnauthorizedAccessException(Path);
                }

                //
                // File In Use
                if (error == Win32Errors.FileInUse) {
                    throw new IOException("File in use", HResults.FileInUse);
                }

                throw new Win32Exception(error);
            }

            return handle;
        }

        private static bool FindReparsePoint(SafeFileHandle handle, out IntPtr reparseDataBuffer, out int reparseDataSize)
        {
            reparseDataBuffer = IntPtr.Zero;
            reparseDataSize = 0;

            IntPtr buff = IntPtr.Zero;
            int dataSize = 0;

            bool result;

            while (true) {
                Marshal.FreeHGlobal(buff);

                int buffSize = Marshal.SizeOf<REPARSE_DATA_BUFFER>() + dataSize;
                buff = Marshal.AllocHGlobal(buffSize);

                int bytesReturned = 0;
                result = DeviceIoControl(handle.DangerousGetHandle(), FSCTL_GET_REPARSE_POINT, IntPtr.Zero, 0, buff, buffSize, out bytesReturned, IntPtr.Zero);

                if (!result) {
                    int error = Marshal.GetLastWin32Error();

                    //
                    // Check if need larger buffer and try again
                    if ((error == ERROR_MORE_DATA || error == ERROR_INSUFFICIENT_BUFFER) && dataSize < REPARSE_DATA_BUFFER.MAX_BUFFER_SIZE) {
                        dataSize += REPARSE_DATA_BUFFER.BUFFER_SIZE;
                        continue;
                    }

                    //
                    // Ignore errors if it's not reparse point
                    if (error != ERROR_PATH_NOT_REPARSE_POINT) {
                        throw new Win32Exception(error);
                    }
                }

                break;
            }

            if (result) {
                reparseDataBuffer = buff;
                reparseDataSize = dataSize;
            }

            return result;
        }

        private static string GetFinalPathNameByHandle(SafeFileHandle handle)
        {
            StringBuilder targetPath = new StringBuilder(260);

            uint size = GetFinalPathNameByHandleW(handle.DangerousGetHandle(), targetPath, (uint)targetPath.Capacity, 0);

            //
            // Insufficient buffer
            if (size > targetPath.Capacity) {
                targetPath.Capacity = (int)size;

                size = GetFinalPathNameByHandleW(handle.DangerousGetHandle(), targetPath, (uint)targetPath.Capacity, 0);
            }

            //
            // Failure
            if (size == 0 || size > targetPath.Capacity) {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            var path = targetPath.ToString();

            if (path.StartsWith(@"\\?\UNC")) {
                return '\\' + path.Substring(7);
            }
            else {
                return path.StartsWith(@"\\?\") ? path.Substring(4) : path;
            }
        }
    }
}
