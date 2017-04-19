// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Certificates
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography.X509Certificates;

    class Interop
    {
        private const int KP_PERMISSIONS = 6;
        private const int CRYPT_EXPORT = 0x0004;
        private const string CRYPT32 = "crypt32.dll";
        private const string CRYPT_API_SET = "cryptsp.dll";

        [DllImport(CRYPT32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool CryptAcquireCertificatePrivateKey(IntPtr pCertContext, uint flags, IntPtr reserved, [Out] out SafeCryptProvHandle phCryptProv, ref uint pdwKeySpec, [MarshalAs(UnmanagedType.Bool)] ref bool pfCallerFreeProv);

        [DllImport(CRYPT_API_SET)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal extern static bool CryptDestroyKey(IntPtr hKey);

        [DllImport(CRYPT_API_SET, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal extern static bool CryptGetKeyParam(IntPtr hKey, int dwParam, SafeGlobalAllocHandle pvData, ref int pcbData, uint dwFlags);

        [DllImport(CRYPT_API_SET, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal extern static bool CryptGetUserKey(SafeCryptProvHandle hCryptProv, uint pdwKeySpec, ref IntPtr hKey);

        [DllImport(CRYPT_API_SET)]
        internal static extern bool CryptReleaseContext(IntPtr hCryptProv, uint dwFlags);

        public static bool IsPrivateKeyExportable(X509Certificate cert)
        {
            bool exportable = false;
            SafeCryptProvHandle hProvider = null;
            IntPtr hKey = IntPtr.Zero;
            bool freeProvider = false;
            bool freeCryptKey = false;
            uint acquireFlags = 0;
            uint keySpec = 0;
            byte[] permissionBytes = null;
            
            if (CryptAcquireCertificatePrivateKey(cert.Handle, acquireFlags, IntPtr.Zero, out hProvider, ref keySpec, ref freeProvider)) {
                SafeGlobalAllocHandle pBytes = SafeGlobalAllocHandle.Empty();
                int length = 0;

                try {
                    if (CryptGetUserKey(hProvider, keySpec, ref hKey)) {
                        freeCryptKey = true;

                        if (CryptGetKeyParam(hKey, KP_PERMISSIONS, pBytes, ref length, 0)) {
                            pBytes = new SafeGlobalAllocHandle(length);

                            if (CryptGetKeyParam(hKey, KP_PERMISSIONS, pBytes, ref length, 0)) {
                                permissionBytes = new byte[length];

                                pBytes.Copy(permissionBytes, 0, length);

                                int cryptNum = BitConverter.ToInt32(permissionBytes, 0);
                                exportable = ((cryptNum & CRYPT_EXPORT) != 0);
                            }
                        }
                    }
                }
                finally {
                    if (freeCryptKey) {
                        CryptDestroyKey(hKey);
                    }

                    if (freeProvider && hProvider != null) {
                        hProvider.Dispose();
                        hProvider = null;
                    }

                    if (pBytes != null) {
                        pBytes.Dispose();
                        pBytes = null;
                    }
                }
            }

            return exportable;
        }
    }

    sealed class SafeGlobalAllocHandle : SafeHandle
    {
        private static readonly IntPtr invalid = new IntPtr(-1);

        public static SafeGlobalAllocHandle Empty()
        {
            return new SafeGlobalAllocHandle();
        }
        
        private SafeGlobalAllocHandle(): base(IntPtr.Zero, true)
        {
            SetHandle(IntPtr.Zero);
        }

        public SafeGlobalAllocHandle(int size) : base(IntPtr.Zero, true)
        {
            SetHandle(Marshal.AllocHGlobal(size));
        }

        public override bool IsInvalid {
            get {
                return handle == IntPtr.Zero || handle == invalid;
            }
        }

        public void Copy(byte[] bytes, int start, int length)
        {
            Marshal.Copy(handle, bytes, start, length);
        }

        protected override bool ReleaseHandle()
        {
            if (!IsInvalid) {
                Marshal.FreeHGlobal(handle);
                SetHandleAsInvalid();
            }

            return true;
        }
    }

    sealed class SafeCryptProvHandle : SafeHandle
    {
        private static readonly IntPtr invalid = new IntPtr(-1);

        private SafeCryptProvHandle() : base(IntPtr.Zero, true) { }

        public SafeCryptProvHandle(IntPtr handle) : base(IntPtr.Zero, true)
        {
            SetHandle(handle);
        }

        public override bool IsInvalid {
            get {
                return handle == IntPtr.Zero || handle == invalid;
            }
        }

        protected override bool ReleaseHandle()
        {
            return Interop.CryptReleaseContext(handle, 0);
        }
    }
}
