// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Logging
{
    using Web.Administration;

    internal class CentralBinaryLogFile : ConfigurationElement {

        private const string DirectoryAttribute = "directory";
        private const string EnabledAttribute = "enabled";
        private const string LocalTimeRolloverAttribute = "localTimeRollover";
        private const string PeriodAttribute = "period";
        private const string TruncateSizeAttribute = "truncateSize";

        public CentralBinaryLogFile() {
        }

        public string Directory {
            get {
                return (string)base[DirectoryAttribute];
            }
            set {
                base[DirectoryAttribute] = value;
            }
        }

        public bool Enabled {
            get {
                return (bool)base[EnabledAttribute];
            }
            set {
                base[EnabledAttribute] = value;
            }
        }

        public bool LocalTimeRollover {
            get {
                return (bool)base[LocalTimeRolloverAttribute];
            }
            set {
                base[LocalTimeRolloverAttribute] = value;
            }
        }

        public LoggingRolloverPeriod Period {
            get {
                return (LoggingRolloverPeriod)base[PeriodAttribute];
            }
            set {
                base[PeriodAttribute] = (int)value;
            }
        }

        public long TruncateSize {
            get {
                return (long)base[TruncateSizeAttribute];
            }
            set {
                base[TruncateSizeAttribute] = value;
            }
        }
    }
}
