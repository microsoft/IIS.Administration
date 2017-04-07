// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Modules
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;

    class PeInfo
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct IMAGE_FILE_HEADER
        {
            public UInt16 Machine;
            public UInt16 NumberOfSections;
            public UInt32 TimeDateStamp;
            public UInt32 PointerToSymbolTable;
            public UInt32 NumberOfSymbols;
            public UInt16 SizeOfOptionalHeader;
            public UInt16 Characteristics;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IMAGE_DOS_HEADER
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public char[] e_magic;       // Magic number
            public UInt16 e_cblp;    // Bytes on last page of file
            public UInt16 e_cp;      // Pages in file
            public UInt16 e_crlc;    // Relocations
            public UInt16 e_cparhdr;     // Size of header in paragraphs
            public UInt16 e_minalloc;    // Minimum extra paragraphs needed
            public UInt16 e_maxalloc;    // Maximum extra paragraphs needed
            public UInt16 e_ss;      // Initial (relative) SS value
            public UInt16 e_sp;      // Initial SP value
            public UInt16 e_csum;    // Checksum
            public UInt16 e_ip;      // Initial IP value
            public UInt16 e_cs;      // Initial (relative) CS value
            public UInt16 e_lfarlc;      // File address of relocation table
            public UInt16 e_ovno;    // Overlay number
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public UInt16[] e_res1;    // Reserved words
            public UInt16 e_oemid;       // OEM identifier (for e_oeminfo)
            public UInt16 e_oeminfo;     // OEM information; e_oemid specific
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public UInt16[] e_res2;    // Reserved words
            public Int32 e_lfanew;      // File address of new exe header

            public bool isValid {
                get { return _e_magic == "MZ"; }
            }

            private string _e_magic {
                get { return new string(e_magic); }
            }
        }

        private readonly IMAGE_FILE_HEADER? _imageFileHeader;

        public PeInfo(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                var reader = new BinaryReader(fs);

                //
                // Read IMAGE_DOS_HEADER
                IMAGE_DOS_HEADER? dosHeader = Read<IMAGE_DOS_HEADER>(reader);
                if (dosHeader != null && !dosHeader.Value.isValid) {
                    //
                    // Not a PE image
                    return;
                }

                reader.BaseStream.Position = fs.Seek(dosHeader.Value.e_lfanew, SeekOrigin.Begin) + Marshal.SizeOf<Int32>();

                _imageFileHeader = Read<IMAGE_FILE_HEADER>(reader);
            }
        }

        public bool IsValid {
            get {
                return _imageFileHeader != null && _imageFileHeader.HasValue;
            }
        }

        public ImageFileMachine Machine {
            get {
                if (!IsValid) {
                    throw new InvalidOperationException();
                }

                return (ImageFileMachine)_imageFileHeader.Value.Machine;
            }
        }


        private static Nullable<T> Read<T>(BinaryReader reader) where T : struct
        {
            int size = Marshal.SizeOf<T>();
            byte[] data = reader.ReadBytes(size);

            if (data == null || data.Length != size) {
                return null;
            }

            // Pin the buffer
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);

            try {
                return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            }
            finally {
                handle.Free();
            }
        }
    }

    enum ImageFileMachine
    {
        I386 = 0x14C,
        ARM = 0x1C4,
        IA64 = 0x200,
        AMD64 = 0x8664
    }
}
