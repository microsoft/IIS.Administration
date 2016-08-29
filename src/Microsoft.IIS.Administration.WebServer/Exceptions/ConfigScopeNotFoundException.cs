// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer {
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Core;


    public class ConfigScopeNotFoundException : Exception, IError {
        private string _message;

        public string ConfigPath { get; private set; }

        public override string Message {
            get {
                return _message;
            }
        }

        public ConfigScopeNotFoundException(DirectoryNotFoundException ex = null) : base(null, ex) {
            IEnumerable<string> data = ex.Message.Split('\r', '\n').Where(s=> !string.IsNullOrWhiteSpace(s)).Select(s=> s.Trim());
            if (data == null) {
                return;
            }

            var path = data.Where(s => s.StartsWith("Filename:", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (path != null) {
                ConfigPath = path.Substring("Filename:".Length).Trim();
            }

            var error = data.Where(s => s.StartsWith("Error:", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (error != null) {
                _message = error.Substring("Error:".Length).Trim();
            }
        }

        public dynamic GetApiError() {
            return ErrorHelper.ConfigScopeNotFoundError(this);
        }
    }
}
