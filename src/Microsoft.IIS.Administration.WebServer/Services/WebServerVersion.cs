// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer
{
    using Win32;
    using System;

    class WebServerVersion : IWebServerVersion
    {
        private const string REGKEY_IIS_PARAMS = @"SYSTEM\CurrentControlSet\Services\W3SVC\Parameters";
        private const string REGVAL_IIS_MAJOR_VERSION = "MajorVersion";
        private const string REGVAL_IIS_MINOR_VERSION = "MinorVersion";

        private Version _version = null;
        private bool _resolved = false;

        /// <summary>
        /// Returns the version of IIS on the machine or null if the version cannot be determined.
        /// </summary>
        /// <returns>The version of IIS on the machine or null if the version cannot be determined.</returns>
        public Version Version
        {
            get {
                if (!_resolved) {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(REGKEY_IIS_PARAMS, false)) {

                        int? major = null, minor = null;

                        if (key != null) {
                            major = (int?)key.GetValue(REGVAL_IIS_MAJOR_VERSION);
                            minor = (int?)key.GetValue(REGVAL_IIS_MINOR_VERSION);
                        }

                        if (major != null && minor != null) {
                            _version = new Version(major.Value, minor.Value);
                        }
                    }

                    _resolved = true;
                }

                return _version;
            }
        }
    }
}
