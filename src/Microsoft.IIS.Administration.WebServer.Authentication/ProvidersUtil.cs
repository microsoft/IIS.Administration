// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;

    static class ProvidersUtil
    {
        private static List<string> nego2AuthProvidersList = new List<string>();

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SecPkgInfo
        {
            public int fCapabilities;
            public short wVersion;
            public short wRPCID;
            public int cbMaxToken;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Name;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Comment;
        }

        [DllImport("secur32.dll", EntryPoint = "EnumerateSecurityPackagesW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int EnumerateSecuirtyPackagesW(out IntPtr pcPackages, //Number of packages
                                                            out IntPtr ppPackageInfo); // Package Information

        [DllImport("secur32.dll", EntryPoint = "FreeContextBuffer", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int FreeContextBuffer(IntPtr Ptr);

        public static List<string> GetAvailableProvidersList()
        {
            List<string> configuredProviders = new List<string>();
            List<string> availableProvidersList = new List<string>();
            try {
                const int SEC_E_OK = 0;
                const int SECPKG_FLAG_NEGOTIABLE2 = 0x00200000;
                const int SECPKG_FLAG_NEGOTIABLE = 0x00000800;
                IntPtr pcPackages = (IntPtr)1;
                IntPtr secPkgInfoPtr;
                int retValue = EnumerateSecuirtyPackagesW(out pcPackages, out secPkgInfoPtr);
                if (retValue != SEC_E_OK) {
                    if (secPkgInfoPtr != null) {
                        retValue = FreeContextBuffer(secPkgInfoPtr);
                        if (retValue != SEC_E_OK) {
                            // Failed to release memory
                        }
                    }
                    return null;
                }
                int nego2FlagIntValue = (int)SECPKG_FLAG_NEGOTIABLE2;
                int negoFlagIntValue = (int)SECPKG_FLAG_NEGOTIABLE;
                IntPtr currentPtrToStruct = secPkgInfoPtr;
                for (int i = 0; i < (int)pcPackages; ++i) {
                    SecPkgInfo currentPackageStruct = (SecPkgInfo)Marshal.PtrToStructure<SecPkgInfo>(currentPtrToStruct);
                    // Ignore NegoExtender package (because NegoExtender is a psuedo authentication protocol)
                    // and show 'pku2u' because it proven that it works over HTTP
                    if ((currentPackageStruct.Name).Equals("NegoExtender", StringComparison.OrdinalIgnoreCase)) {
                        // Go to the next available struct
                        currentPtrToStruct = (IntPtr)((Int64)currentPtrToStruct + Marshal.SizeOf<SecPkgInfo>());
                        continue;
                    }
                    if (((currentPackageStruct.fCapabilities & nego2FlagIntValue) == 0) && ((currentPackageStruct.fCapabilities & negoFlagIntValue) == 0)) {
                        currentPtrToStruct = (IntPtr)((Int64)currentPtrToStruct + Marshal.SizeOf<SecPkgInfo>());
                        continue;
                    }
                    string strPackageName = null;
                    // 1. If this is a Negotiable2 package, prefix package name with 'Negotiate:'                
                    // 2. If this is a Negotiable package and it's name is not 'Negotiate' and 'NTLM' , then add prefix 'Negotiate:' to the pacakge name
                    // IMPORTANT NOTE: Runtime sends Nego2 package when one of the enabled provider is prefixed with 'Negotiate:' (which is considered as Nego2 package)
                    //    Just to be consistent with the runtime behavior we will display a warning message for all the Negotiate packages which are prefixed with 'Negotiate:'
                    if (((currentPackageStruct.fCapabilities & nego2FlagIntValue) != 0)
                        || ((currentPackageStruct.fCapabilities & negoFlagIntValue) != 0
                            && !(currentPackageStruct.Name).Equals("Negotiate", StringComparison.OrdinalIgnoreCase)
                            && !(currentPackageStruct.Name).Equals("NTLM", StringComparison.OrdinalIgnoreCase)
                            )
                        ) {
                        strPackageName = "Negotiate:";
                    }
                    strPackageName += currentPackageStruct.Name;

                    // Add it to the available providers list
                    availableProvidersList.Add(strPackageName);

                    // Go to the next available struct
                    currentPtrToStruct = (IntPtr)((Int64)currentPtrToStruct + Marshal.SizeOf<SecPkgInfo>());
                }
                // Free the memory
                retValue = FreeContextBuffer(secPkgInfoPtr);
                if (retValue != SEC_E_OK) {
                    // Failed to release the memory...
                }
            }
            catch (Exception) { }
            return availableProvidersList;
        }
    }
}
