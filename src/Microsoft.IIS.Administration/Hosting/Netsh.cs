// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography.X509Certificates;

    class Interop
    {
        public const int HTTP_INITIALIZE_CONFIG = 2;
        public const int HTTP_SERVICE_CONFIG_SSLCERT_INFO = 1;

        [DllImport("httpapi.dll", CharSet = CharSet.Auto, PreserveSig = true)]
        public static extern uint HttpDeleteServiceConfiguration(IntPtr ServiceHandle, int ConfigId, ref HTTP_SERVICE_CONFIG_SSL_SET pConfigInformation, int ConfigInformationLength, IntPtr pOverlapped);

        [DllImport("httpapi.dll", CharSet = CharSet.Auto, PreserveSig = true)]
        public static extern int HttpInitialize(HTTPAPI_VERSION version, uint flags, IntPtr pReserved);

        [DllImport(
            "httpapi.dll",
            EntryPoint = "HttpQueryServiceConfiguration",
            CharSet = CharSet.Unicode, ExactSpelling = true,
            CallingConvention = CallingConvention.StdCall)]
        public static extern int HttpQueryServiceConfiguration(
            IntPtr serviceHandle,
            HTTP_SERVICE_CONFIG_ID configID,
            ref HTTP_SERVICE_CONFIG_SSL_QUERY pInputConfigInfo,
            UInt32 InputConfigInfoLength,
            IntPtr pOutputConfigInfo,
            UInt32 OutputConfigInfoLength,
            [In, Out] ref UInt32 pReturnLength,
            IntPtr pOverlapped
        );

        [DllImport("httpapi.dll", CharSet = CharSet.Auto, PreserveSig = true)]
        public static extern int HttpSetServiceConfiguration(IntPtr ServiceHandle, int ConfigId, ref HTTP_SERVICE_CONFIG_SSL_SET pConfigInformation, int ConfigInformationLength, IntPtr pOverlapped);

        [DllImport("httpapi.dll", CharSet = CharSet.Auto, PreserveSig = true)]
        public static extern uint HttpTerminate(uint flags, IntPtr pReserved);

        public static HTTP_SERVICE_CONFIG_SSL_SET MarshalConfigSslSet(IntPtr ptr)
        {
            return (HTTP_SERVICE_CONFIG_SSL_SET)Marshal.PtrToStructure(ptr, typeof(HTTP_SERVICE_CONFIG_SSL_SET));
        }
    }

    public class HttpsBindingInfo
    {
        public byte[] CertificateHash { get; set; }
        public Guid ApplicationId { get; set; }
        public IPEndPoint Endpoint { get; set; }
    }


    public enum HTTP_SERVICE_CONFIG_ID
    {
        HttpServiceConfigIPListenList,
        HttpServiceConfigSSLCertInfo,
        HttpServiceConfigUrlAclInfo,
        HttpServiceConfigMax
    }

    public enum HTTP_SERVICE_CONFIG_QUERY_TYPE
    {
        HttpServiceConfigQueryExact,
        HttpServiceConfigQueryNext,
        HttpServiceConfigQueryMax
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HTTPAPI_VERSION
    {
        public ushort HttpApiMajorVersion;
        public ushort HttpApiMinorVersion;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct HTTP_SERVICE_CONFIG_SSL_KEY
    {
        public IntPtr pIpPort;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct HTTP_SERVICE_CONFIG_SSL_QUERY
    {
        public HTTP_SERVICE_CONFIG_QUERY_TYPE QueryDesc;
        public IntPtr KeyDesc;
        public Int32 dwToken;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HTTP_SERVICE_CONFIG_SSL_SET
    {
        public IntPtr KeyDesc;
        public uint SslHashLength;
        public IntPtr pSslHash;
        public Guid AppId;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pSslCertStoreName;
        public int DefaultCertCheckMode;
        public int DefaultRevocationFreshnessTime;
        public int DefaultRecovationUrlRetrievalTimeout;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pDefaultSslCtlIdentifier;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pDefaultSslCtlStoreName;
        public int DefaultFlags;
    }

    class Netsh
    {
        private static int ERROR_NO_MORE_ITEMS = 259;

        private class HttpSysContext : IDisposable
        {
            public HttpSysContext()
            {
                InitializeHttpSys();
            }

            public void Dispose()
            {
                TerminateHttpSys();
            }
        }

        public static void InitializeHttpSys()
        {
            var v = new HTTPAPI_VERSION();

            v.HttpApiMajorVersion = 1;
            v.HttpApiMinorVersion = 0;

            int result = Interop.HttpInitialize(v, Interop.HTTP_INITIALIZE_CONFIG, IntPtr.Zero);

            if (result != 0)
            {
                throw new Win32Exception(result);
            }
        }

        public static void TerminateHttpSys()
        {
            Interop.HttpTerminate(Interop.HTTP_INITIALIZE_CONFIG, IntPtr.Zero);
        }

        public static byte[] GetIpEndpointBytes(IPEndPoint endpoint)
        {
            var socketAddress = endpoint.Serialize();

            var ipBytes = new byte[socketAddress.Size];

            for (int i = 0; i < socketAddress.Size; i++)
            {
                ipBytes[i] = socketAddress[i];
            }

            return ipBytes;
        }

        public static HttpsBindingInfo GetSslBinding(IPEndPoint endpoint)
        {
            const int bufferSize = 4096;

            using (var context = new HttpSysContext())
            {
                var ipBytes = GetIpEndpointBytes(endpoint);

                var hIp = GCHandle.Alloc(ipBytes, GCHandleType.Pinned);

                try
                {
                    var pIp = hIp.AddrOfPinnedObject();

                    var queryParam = new HTTP_SERVICE_CONFIG_SSL_QUERY();
                    queryParam.QueryDesc = HTTP_SERVICE_CONFIG_QUERY_TYPE.HttpServiceConfigQueryExact;
                    queryParam.dwToken = 0;
                    queryParam.KeyDesc = pIp;

                    uint returnLen = 0;
                    var pReturnSet = Marshal.AllocHGlobal(bufferSize);

                    try
                    {
                        var result = Interop.HttpQueryServiceConfiguration(
                                IntPtr.Zero,
                                HTTP_SERVICE_CONFIG_ID.HttpServiceConfigSSLCertInfo,
                                ref queryParam,
                                (uint)Marshal.SizeOf<HTTP_SERVICE_CONFIG_SSL_QUERY>(),
                                pReturnSet,
                                bufferSize,
                                ref returnLen,
                                IntPtr.Zero
                            );

                        if (result == 2)
                        {
                            // File not found
                            return null;
                        }
                        if (result != 0)
                        {
                            throw new Win32Exception(result);
                        }

                        return ToHttpsBindingInfo(Interop.MarshalConfigSslSet(pReturnSet));
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(pReturnSet);
                    }

                }
                finally
                {
                    hIp.Free();
                }
            }
        }

        public static IEnumerable<HttpsBindingInfo> GetSslBindings()
        {
            const int bufferSize = 4096;
            int result = 0;
            int index = 0;
            var bindings = new List<HttpsBindingInfo>();

            using (var context = new HttpSysContext())
            {
                while (result == 0)
                {
                    var queryParam = new HTTP_SERVICE_CONFIG_SSL_QUERY();
                    queryParam.QueryDesc = HTTP_SERVICE_CONFIG_QUERY_TYPE.HttpServiceConfigQueryNext;
                    queryParam.dwToken = index;
                    queryParam.KeyDesc = IntPtr.Zero;

                    uint returnLen = 0;
                    var pReturnSet = Marshal.AllocHGlobal(bufferSize);

                    try
                    {
                        result = Interop.HttpQueryServiceConfiguration(
                                IntPtr.Zero,
                                HTTP_SERVICE_CONFIG_ID.HttpServiceConfigSSLCertInfo,
                                ref queryParam,
                                (uint)Marshal.SizeOf<HTTP_SERVICE_CONFIG_SSL_QUERY>(),
                                pReturnSet,
                                bufferSize,
                                ref returnLen,
                                IntPtr.Zero
                            );

                        if (result == 2)
                        {
                            // File not found
                            return null;
                        }
                        if (result == Netsh.ERROR_NO_MORE_ITEMS)
                        {
                            continue;
                        }
                        if (result != 0)
                        {
                            throw new Win32Exception(result);
                        }

                        index++;
                        bindings.Add(ToHttpsBindingInfo(Interop.MarshalConfigSslSet(pReturnSet)));
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(pReturnSet);
                    }
                }
            }

            return bindings;
        }

        public static HttpsBindingInfo ToHttpsBindingInfo(HTTP_SERVICE_CONFIG_SSL_SET sslConfig)
        {
            byte[] certificateHash = new byte[sslConfig.SslHashLength];

            Marshal.Copy(sslConfig.pSslHash, certificateHash, 0, (int)sslConfig.SslHashLength);

            const int socketAddressLength = 16;

            var sa = new byte[socketAddressLength];

            Marshal.Copy(sslConfig.KeyDesc, sa, 0, socketAddressLength);

            var socketAddress = new SocketAddress(System.Net.Sockets.AddressFamily.InterNetwork, socketAddressLength);

            for (int i = 0; i < sa.Length; i++)
            {
                socketAddress[i] = sa[i];
            }

            var ep = new IPEndPoint(IPAddress.Any, 0);

            var endpoint = (IPEndPoint)ep.Create(socketAddress);

            return new HttpsBindingInfo()
            {
                CertificateHash = certificateHash,
                Endpoint = endpoint,
                ApplicationId = sslConfig.AppId
            };
        }

        public static void SetHttpsBinding(IPEndPoint endpoint, X509Certificate2 cert, Guid appId)
        {
            using (var context = new HttpSysContext())
            {
                var sslSet = new HTTP_SERVICE_CONFIG_SSL_SET();
                var size = Marshal.SizeOf<HTTP_SERVICE_CONFIG_SSL_SET>();

                var ipBytes = GetIpEndpointBytes(endpoint);
                var hIp = GCHandle.Alloc(ipBytes, GCHandleType.Pinned);

                try
                {
                    var pIp = hIp.AddrOfPinnedObject();

                    sslSet.KeyDesc = pIp;
                    sslSet.SslHashLength = 0;
                    sslSet.pSslHash = IntPtr.Zero;
                    sslSet.pSslCertStoreName = null;
                    sslSet.AppId = appId;

                    var certBytes = cert.GetCertHash();
                    var hCertBytes = GCHandle.Alloc(certBytes, GCHandleType.Pinned);

                    try
                    {
                        var pCertBytes = hCertBytes.AddrOfPinnedObject();

                        sslSet.SslHashLength = 20;
                        sslSet.pSslHash = pCertBytes;
                        sslSet.pSslCertStoreName = "MY";

                        int result = Interop.HttpSetServiceConfiguration(
                            IntPtr.Zero,
                            Interop.HTTP_SERVICE_CONFIG_SSLCERT_INFO,
                            ref sslSet,
                            size,
                            IntPtr.Zero
                        );

                        if (result != 0)
                        {
                            throw new Win32Exception(result);
                        }
                    }
                    finally
                    {
                        hCertBytes.Free();
                    }
                }
                finally
                {
                    hIp.Free();
                }
            }
        }
    }
}
