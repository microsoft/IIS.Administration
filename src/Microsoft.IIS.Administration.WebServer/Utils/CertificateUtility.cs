// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Utils
{
    using System;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography.X509Certificates;

    public static class CertificateUtility
    {
        public static X509Certificate2 CreateCertificateFromFile(string filePath)
        {
            CertEncodingType encoding;
            ContentType contentType;
            FormatType formatType;
            IntPtr hCertStore;
            IntPtr hMsg;
            IntPtr pvContext;

            if (!NativeMethods.CryptQueryObject(
                CertQueryObjectType.CERT_QUERY_OBJECT_FILE,
                filePath,
                ExpectedContentTypeFlags.CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED_EMBED,
                ExpectedFormatTypeFlags.CERT_QUERY_FORMAT_FLAG_BINARY,
                0,
                out encoding,
                out contentType,
                out formatType,
                out hCertStore,
                out hMsg,
                out pvContext)) {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            pvContext = NativeMethods.CertFindCertificateInStore(
                hCertStore,
                CertEncodingType.All,
                CertFindFlags.None,
                CertFindType.CERT_FIND_ANY,
                IntPtr.Zero,
                IntPtr.Zero);

            if (pvContext == IntPtr.Zero) {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            return new X509Certificate2(pvContext);
        }

        private static class NativeMethods
        {
            [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern bool CryptQueryObject(
                CertQueryObjectType dwObjectType,
                [MarshalAs(UnmanagedType.LPWStr)]
            string pvObject,
                ExpectedContentTypeFlags dwExpectedContentTypeFlags,
                ExpectedFormatTypeFlags dwExpectedFormatTypeFlags,
                int dwFlags, // reserved - always pass 0
                out CertEncodingType pdwMsgAndCertEncodingType,
                out ContentType pdwContentType,
                out FormatType pdwFormatType,
                out IntPtr phCertStore,
                out IntPtr phMsg,
                out IntPtr ppvContext
                );

            [DllImport("crypt32.dll", SetLastError = true)]
            internal static extern IntPtr CertFindCertificateInStore(
                IntPtr hCertStore,
                CertEncodingType dwCertEncodingType,
                CertFindFlags dwFindFlags,
                CertFindType dwFindType,
                IntPtr pszFindPara,
                IntPtr pPrevCertCntxt);
        }

        enum CertQueryObjectType : int
        {
            CERT_QUERY_OBJECT_FILE = 0x00000001
        }

        [Flags]
        enum ExpectedContentTypeFlags : int
        {
            CERT_QUERY_CONTENT_FLAG_PKCS7_SIGNED_EMBED = 1 << ContentType.CERT_QUERY_CONTENT_PKCS7_SIGNED_EMBED
        }

        [Flags]
        enum ExpectedFormatTypeFlags : int
        {
            CERT_QUERY_FORMAT_FLAG_BINARY = 1 << FormatType.CERT_QUERY_FORMAT_BINARY
        }

        enum CertEncodingType : int
        {
            PKCS_7_ASN_ENCODING = 0x10000,
            X509_ASN_ENCODING = 0x00001,

            All = PKCS_7_ASN_ENCODING | X509_ASN_ENCODING
        }

        enum ContentType : int
        {
            CERT_QUERY_CONTENT_PKCS7_SIGNED_EMBED = 10
        }

        enum FormatType : int
        {
            CERT_QUERY_FORMAT_BINARY = 1
        }

        [Flags]
        enum CertFindFlags : int
        {
            None = 0x00000000
        }

        enum CertFindType : int
        {
            CERT_FIND_ANY = 0x00000000
        }
    }
}
