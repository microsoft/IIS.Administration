// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer
{
    using System;
    using System.Runtime.InteropServices;
    using System.IO;

    public static class BitnessUtility {

        public static void AppendBitnessPreCondition(ref string preCondition, string filePath) {
            if (Is32BitMachine()) {
                return;
            }

            if (Is32Bit(filePath)) {
                if (!preCondition.ToLower().Contains("bitness32")) {
                    if (preCondition.ToLower().Contains("bitness64")) {
                        preCondition = preCondition.Replace("bitness64", "bitness32");
                    }
                    else {
                        preCondition = preCondition + ",bitness32";
                    }
                }
            }
            else {
                if (!preCondition.ToLower().Contains("bitness64")) {
                    if (preCondition.ToLower().Contains("bitness32")) {
                        preCondition = preCondition.Replace("bitness32", "bitness64");
                    }
                    else {
                        preCondition = preCondition + ",bitness64";
                    }
                }
            }

            preCondition = preCondition.Trim(',');
        }

        public static bool Is32BitMachine() {
            unsafe {
                return (sizeof(IntPtr) == 4);
            }
        }

        private static bool Is32Bit(string filePath) {

            filePath = Environment.ExpandEnvironmentVariables(filePath);

            // Read the first
            byte[] data = new byte[4096];

            FileInfo fileInfo = new FileInfo(filePath);
            using (FileStream fileStream = fileInfo.Open(FileMode.Open, FileAccess.Read)) {

                fileStream.Read(data, 0, 4096);

                fileStream.Flush();
            }

            unsafe {
                fixed (byte* p_Data = data) {
                    // Get the first 64 bytes and turn it into a IMAGE_DOS_HEADER
                    IMAGE_DOS_HEADER* idh = (IMAGE_DOS_HEADER*)p_Data;

                    // Now that we have the DOS header, we can get the offset
                    // (e_lfanew) add it to the original address (p_Data) and 
                    // squeeze those bytes into a IMAGE_NT_HEADERS32 structure
                    IMAGE_NT_HEADERS32* inhs = (IMAGE_NT_HEADERS32*)(idh->e_lfanew + p_Data);

                    // Use the OptionalHeader.Magic. It tells you whether
                    // the assembly is PE32 (0x10b) or PE32+ (0x20b). 
                    // PE32+ just means 64-bit. So, instead of checking if it is 
                    // an X64 or Itanium, just check if it's a PE32+.
                    if (inhs->OptionalHeader.Magic == 0x20b) {
                        // 64 bit assembly
                        return false;
                    }
                    else {
                        // 32 bit assembly
                        return true;
                    }
                }
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct IMAGE_DOS_HEADER {
            [FieldOffset(60)]
            public int e_lfanew;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct IMAGE_NT_HEADERS32 {
            [FieldOffset(24)]
            public IMAGE_OPTIONAL_HEADER32 OptionalHeader;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct IMAGE_NT_HEADERS64 {
            [FieldOffset(24)]
            public IMAGE_OPTIONAL_HEADER64 OptionalHeader;
        }

        [StructLayout(LayoutKind.Explicit, Size=20)]
        private struct IMAGE_FILE_HEADER {
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct IMAGE_OPTIONAL_HEADER32 {
            [FieldOffset(0)]
            public ushort Magic;
            [FieldOffset(208)]
            public IMAGE_DATA_DIRECTORY DataDirectory;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct IMAGE_OPTIONAL_HEADER64 {
            [FieldOffset(0)]
            public ushort Magic;
            [FieldOffset(224)]
            public IMAGE_DATA_DIRECTORY DataDirectory;
        }

        [StructLayout(LayoutKind.Explicit, Size = 8)]
        private struct IMAGE_DATA_DIRECTORY {
        }
    }
}
