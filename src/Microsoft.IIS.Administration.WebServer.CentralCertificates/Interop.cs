// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.CentralCertificates
{
    using System;
    using System.Runtime.InteropServices;
    using Win32.SafeHandles;

    class Interop
    {
        private const string CRYPT_API_SET = "cryptsp.dll";
        private const string SECURITY_API_SET = "sspicli.dll";

        public const int NTE_EXISTS = unchecked((int)0x8009000F);
        public const uint CRYPT_NEWKEYSET = 0x00000008;
        public const uint CRYPT_MACHINE_KEYSET = 0x00000020;
        public const int PROV_RSA_AES = 24;
        public const uint AT_KEYEXCHANGE = 1;
        public const int ERROR_MORE_DATA = 234;
        public const int LOGON32_PROVIDER_DEFAULT = 0;
        //This parameter causes LogonUser to create a primary token. 
        public const int LOGON32_LOGON_INTERACTIVE = 2;

        [DllImport(CRYPT_API_SET, CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool CryptAcquireContextW(
            [Out]    out SafeCryptProvHandle hCryptProv,
            [In]     [MarshalAs(UnmanagedType.LPWStr)] string pszContainer,
            [In]     [MarshalAs(UnmanagedType.LPWStr)] string pszProvider,
            [In]     uint dwProvType,
            [In]     uint dwFlags);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CryptDecrypt(IntPtr hKey,
            IntPtr hHash,
            [MarshalAsAttribute(UnmanagedType.Bool)] bool Final,
            uint dwFlags,
            byte[] pbData,
            ref int pdwDataLen);

        [DllImport(CRYPT_API_SET, CharSet = CharSet.Unicode)]
        public extern static bool CryptDestroyKey(
            [In] IntPtr hKey);

        [DllImport(CRYPT_API_SET, SetLastError = true)]
        public static extern bool CryptEncrypt(
            IntPtr hKey,
            IntPtr hHash,
            bool Final,
            uint dwFlags,
            byte[] pbData,
            ref int pdwDataLen,
            int dwBufLen);

        [DllImport(CRYPT_API_SET, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public extern static bool CryptGetUserKey(SafeCryptProvHandle hCryptProv, uint pdwKeySpec, ref IntPtr hKey);

        [DllImport(CRYPT_API_SET)]
        public static extern bool CryptReleaseContext(IntPtr hCryptProv, uint dwFlags);

        [DllImport(SECURITY_API_SET, SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool LogonUserExExW(
            String lpszUsername, 
            String lpszDomain,
            String lpszPassword,
            int dwLogonType,
            int dwLogonProvider,
            IntPtr pTokenGroups,
            out SafeAccessTokenHandle phToken,
            IntPtr ppLogonSid,
            IntPtr ppProfileBuffer,
            IntPtr pdwProfileLength,
            IntPtr pQuotaLimits);
    }
}
