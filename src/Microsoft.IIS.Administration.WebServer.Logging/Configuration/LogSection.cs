// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Logging
{
    using Microsoft.Web.Administration;

    internal sealed class LogSection : ConfigurationSection {

        private const string CentralLogFileModeAttribute = "centralLogFileMode";
        private const string LogInUTF8Attribute = "logInUTF8";

        private const string CentralBinaryLogFileElement = "centralBinaryLogFile";
        private const string CentralW3CLogFileElement = "centralW3CLogFile";

        private CentralBinaryLogFile _binaryLogFile;
        private CentralW3CLogFile _w3cLogFile;

        public LogSection() {
        }

        public CentralBinaryLogFile CentralBinaryLogFile {
            get {
                if (_binaryLogFile == null) {
                    _binaryLogFile = (CentralBinaryLogFile)GetChildElement(CentralBinaryLogFileElement, typeof(CentralBinaryLogFile));
                }

                return _binaryLogFile;
            }
        }

        public CentralLogFileMode CentralLogFileMode {
            get {
                return (CentralLogFileMode)base[CentralLogFileModeAttribute];
            }
            set {
                base[CentralLogFileModeAttribute] = (int)value;
            }
        }

        public CentralW3CLogFile CentralW3CLogFile {
            get {
                if (_w3cLogFile == null) {
                    _w3cLogFile = (CentralW3CLogFile)GetChildElement(CentralW3CLogFileElement, typeof(CentralW3CLogFile));
                }

                return _w3cLogFile;
            }
        }

        public bool LogInUTF8 {
            get {
                return (bool)base[LogInUTF8Attribute];
            }
            set {
                base[LogInUTF8Attribute] = value;
            }
        }
    }
}
