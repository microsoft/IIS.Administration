// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Logging
{
    using Microsoft.Web.Administration;

    internal sealed class CentralW3CLogFile : CentralBinaryLogFile {

        private const string LogExtFileFlagsAttribute = "logExtFileFlags";

        public CentralW3CLogFile() {
        }

        public LogExtFileFlags LogExtFileFlags {
            get {
                return (LogExtFileFlags)base[LogExtFileFlagsAttribute];
            }
            set {
                base[LogExtFileFlagsAttribute] = (int)value;
            }
        }
    }
}
