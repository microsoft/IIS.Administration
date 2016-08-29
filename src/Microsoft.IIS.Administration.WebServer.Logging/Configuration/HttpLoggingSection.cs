// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Logging
{
    using Microsoft.Web.Administration;
    
    public sealed class HttpLoggingSection : ConfigurationSection {

        private const string DontLogAttribute = "dontLog";

        public HttpLoggingSection() {
        }

        public bool DontLog {
            get {
                return (bool)base[DontLogAttribute];
            }
            set {
                base[DontLogAttribute] = value;
            }
        }
    }
}
