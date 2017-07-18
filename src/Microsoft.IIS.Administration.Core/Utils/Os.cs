// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core.Utils
{
    using Microsoft.Win32;

    public static class Os
    {
        private const string REG_KEY_OS_EDITION = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";

        private static string _edition;

        public static bool IsNanoServer {
            get {
                return Edition.ToLowerInvariant().Contains("nano");
            }
        }

        private static string Edition {
            get {
                if (_edition == null) {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(REG_KEY_OS_EDITION, false)) {
                        _edition = (string)key.GetValue("EditionID");
                    }
                }

                return _edition;
            }
        }
    }
}
