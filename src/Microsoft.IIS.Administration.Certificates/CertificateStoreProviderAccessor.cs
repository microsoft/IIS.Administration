// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Certificates
{
    using System;

    public class CertificateStoreProviderAccessor
    {
        private static ICertificateStoreProvider _instance;

        internal static IServiceProvider Services { get; set; }

        public static ICertificateStoreProvider Instance {
            get {
                if (_instance != null) {
                    return _instance;
                }

                _instance = (ICertificateStoreProvider)Services?.GetService(typeof(ICertificateStoreProvider));
                return _instance;
            }
        }
    }
}
