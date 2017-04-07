// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.CentralCertificates
{
    using System;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;
    using System.Text;

    public class Crypto
    {
        private const string WAS_KEY_CONTAINER = "iisWasKey";

        public static byte[] Encrypt(string value)
        {
            using (SafeCryptProvHandle provider = AcquireMachineContext(WAS_KEY_CONTAINER, "", Interop.PROV_RSA_AES)) {

                IntPtr key = IntPtr.Zero;
                try {
                    bool result = Interop.CryptGetUserKey(provider, Interop.AT_KEYEXCHANGE, ref key);
                    if (!result) {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }

                    byte[] data = Encoding.Unicode.GetBytes(value);
                    int length = data.Length;

                    result = Interop.CryptEncrypt(key, IntPtr.Zero, true, 0, data, ref length, 0);
                    if (!result) {
                        int err = Marshal.GetLastWin32Error();
                        if (err != Interop.ERROR_MORE_DATA) {
                            throw new Win32Exception(err);
                        }
                    }

                    byte[] encrypted = new byte[length];
                    data.CopyTo(encrypted, 0);
                    length = data.Length;

                    result = Interop.CryptEncrypt(key, IntPtr.Zero, true, 0, encrypted, ref length, encrypted.Length);
                    if (!result) {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }

                    return encrypted;
                }
                finally {
                    if (key != IntPtr.Zero) {
                        Interop.CryptDestroyKey(key);
                    }
                }
            }
        }

        public static string Decrypt(byte[] encrypted)
        {
            using (SafeCryptProvHandle hProvider = AcquireMachineContext(WAS_KEY_CONTAINER, "", Interop.PROV_RSA_AES)) {

                IntPtr hKey = IntPtr.Zero;
                try {
                    bool result = Interop.CryptGetUserKey(hProvider, Interop.AT_KEYEXCHANGE, ref hKey);
                    if (!result) {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }

                    byte[] pbData = new byte[encrypted.Length];
                    encrypted.CopyTo(pbData, 0);

                    int pdwDataLen = pbData.Length;

                    result = Interop.CryptDecrypt(hKey, IntPtr.Zero, true, 0, pbData, ref pdwDataLen);
                    if (!result) {
                        int err = Marshal.GetLastWin32Error();
                        throw new Win32Exception(err);
                    }

                    return Encoding.Unicode.GetString(pbData, 0, pdwDataLen);
                }
                finally {
                    if (hKey != IntPtr.Zero) {
                        Interop.CryptDestroyKey(hKey);
                    }
                }
            }
        }

        private static SafeCryptProvHandle AcquireMachineContext(string keyContainerName, string providerName, uint providerType)
        {
            SafeCryptProvHandle hProv;

            UInt32 dwFlags = Interop.CRYPT_MACHINE_KEYSET;

            bool rc = Interop.CryptAcquireContextW(out hProv,
                                           keyContainerName,
                                           providerName,
                                           providerType,
                                           dwFlags | Interop.CRYPT_NEWKEYSET);

            // Handle key container already exists
            if (!rc) {
                rc = Interop.CryptAcquireContextW(out hProv,
                                          keyContainerName,
                                          providerName,
                                          (uint)providerType,
                                          dwFlags);

                if (!rc) {
                    int error = Marshal.GetLastWin32Error();
                    if (error != Interop.NTE_EXISTS) {
                        throw new CryptographicException(error);
                    }
                }
            }

            return hProv;
        }
    }
}
