# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license. See LICENSE file in the project root for full license information.


Param (
    [parameter(Mandatory=$true , Position=0)]
    [ValidateSet("Enable-AclUtil")]
    [string]
    $Command
)

$cs = '
    namespace Microsoft.IIS.Administration.Setup
    {
        using System;
        using System.ComponentModel;
        using System.Runtime.InteropServices;

        public class AclUtil
        {
            public const string TAKE_OWNERSHIP_PRIVILEGE = "SeTakeOwnershipPrivilege";
            public const string RESTORE_PRIVILEGE = "SeRestorePrivilege";

            internal const int SE_PRIVILEGE_DISABLED = 0x00000000;
            internal const int SE_PRIVILEGE_ENABLED = 0x00000002;
            internal const int TOKEN_QUERY = 0x00000008;
            internal const int TOKEN_ADJUST_PRIVILEGES = 0x00000020;

            private const string ADVAPI = "advapi32.dll";
            private const string KERNEL32 = "kernel32.dll";

            [DllImport(ADVAPI, SetLastError = true)]
            private static extern bool AdjustTokenPrivileges(
                IntPtr hTokenHandle,
                bool fDisableAllPrivileges,
                ref TokPriv1Luid pNewState,
                int nBufferLength,
                IntPtr pPreviousState,
                IntPtr pnReturnLength);

            [DllImport(KERNEL32, SetLastError = true)]
            private static extern bool CloseHandle(IntPtr hObject);

            [DllImport(KERNEL32)]
            private static extern IntPtr GetCurrentProcess();

            [DllImport(ADVAPI, SetLastError = true)]
            private static extern bool GetTokenInformation(
                IntPtr TokenHandle,
                TOKEN_INFORMATION_CLASS TokenInformationClass,
                IntPtr TokenInformation,
                uint TokenInformationLength,
                out uint ReturnLength);

            [DllImport(ADVAPI, SetLastError = true, CharSet = CharSet.Unicode)]
            private static extern bool LookupPrivilegeValueW(
                string host,
                string name,
                ref long pluid);

            [DllImport(ADVAPI, ExactSpelling = true, SetLastError = true)]
            private static extern bool OpenProcessToken(
                IntPtr hProcess,
                int TokenAccess,
                ref IntPtr phAccessToken);

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            private struct TokPriv1Luid
            {
                public int Count;
                public long Luid;
                public int Attr;
            }

            [StructLayout(LayoutKind.Sequential)]
            private class TOKEN_PRIVILEGES
            {
                public UInt32 PrivilegeCount;
                public LUID_AND_ATTRIBUTES[] Privileges;
            }

            [StructLayout(LayoutKind.Sequential, Pack = 4)]
            private class LUID_AND_ATTRIBUTES
            {
                public long Luid;
                public UInt32 Attributes;
            }

            private enum TOKEN_INFORMATION_CLASS
            {
                TokenUser = 1,
                TokenGroups,
                TokenPrivileges
            }

            public static void SetPrivilege(string privilege, bool enabled)
            {
                IntPtr hAccessToken = IntPtr.Zero;
                int attribute = enabled ? SE_PRIVILEGE_ENABLED : SE_PRIVILEGE_DISABLED;

                try {
                    IntPtr hProcess = GetCurrentProcess();

                    if (!OpenProcessToken(hProcess, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, ref hAccessToken)) {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }

                    TokPriv1Luid tp = new TokPriv1Luid();
                    tp.Count = 1;
                    tp.Luid = 0;
                    tp.Attr = attribute;

                    if (!LookupPrivilegeValueW(null, privilege, ref tp.Luid)) {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }

                    if (!AdjustTokenPrivileges(hAccessToken, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero)) {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                }
                finally {
                    if (hAccessToken != IntPtr.Zero) {
                        CloseHandle(hAccessToken);
                        hAccessToken = IntPtr.Zero;
                    }
                }
            }

            public static bool HasPrivilege(string privilege)
            {
                IntPtr hAccessToken = IntPtr.Zero;
                IntPtr pTokenInfo = IntPtr.Zero;

                try {
                    long luid = 0;

                    if (!LookupPrivilegeValueW(null, privilege, ref luid)) {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }

                    IntPtr hProcess = GetCurrentProcess();

                    if (!OpenProcessToken(hProcess, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, ref hAccessToken)) {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }

                    //
                    // Query information size
                    uint cb;
                    if (!GetTokenInformation(hAccessToken, TOKEN_INFORMATION_CLASS.TokenPrivileges, pTokenInfo, 0, out cb)) {
                        pTokenInfo = Marshal.AllocHGlobal((int)cb);

                        if (!GetTokenInformation(hAccessToken, TOKEN_INFORMATION_CLASS.TokenPrivileges, pTokenInfo, cb, out cb)) {
                            throw new Win32Exception(Marshal.GetLastWin32Error());
                        }
                    }

                    TOKEN_PRIVILEGES privileges = MarshalTokenPrivileges(pTokenInfo);

                    foreach (LUID_AND_ATTRIBUTES priv in privileges.Privileges) {
                        if (priv.Luid == luid) {
                            return (priv.Attributes & SE_PRIVILEGE_ENABLED) > 0;
                        }
                    }

                    return false;
                }
                finally {
                    if (hAccessToken != IntPtr.Zero) {
                        CloseHandle(hAccessToken);
                        hAccessToken = IntPtr.Zero;
                    }

                    if (pTokenInfo != IntPtr.Zero) {
                        Marshal.FreeHGlobal(pTokenInfo);
                        pTokenInfo = IntPtr.Zero;
                    }
                }
            }


            private static TOKEN_PRIVILEGES MarshalTokenPrivileges(IntPtr buffer)
            {
                LUID_AND_ATTRIBUTES[] privs = new LUID_AND_ATTRIBUTES[(uint)Marshal.ReadInt32(buffer)];

                for (int i = 0; i < privs.Length; i++) {
                    privs[i] = new LUID_AND_ATTRIBUTES();
                    Marshal.PtrToStructure(new IntPtr(buffer.ToInt64() + sizeof(uint) + Marshal.SizeOf(typeof(LUID_AND_ATTRIBUTES)) * i), privs[i]);
                }

                TOKEN_PRIVILEGES privileges = new TOKEN_PRIVILEGES();
                privileges.PrivilegeCount = (uint)privs.Length;
                privileges.Privileges = privs;

                return privileges;
            }
        }
    }
'

function InitializeInterop() {
    try {
        [Microsoft.IIS.Administration.Setup.AclUtil] | Out-Null
    }
    catch {
        Add-Type $cs
    }
}

switch ($Command)
{
    "Enable-AclUtil"
    {
        InitializeInterop
    }
    default
    {
        throw "Unknown command"
    }
}