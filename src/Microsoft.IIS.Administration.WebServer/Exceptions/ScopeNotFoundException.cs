// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer {
    using System;
    using Core;


    public class ScopeNotFoundException : Exception, IError {
        public string Scope { get; private set; }

        public ScopeNotFoundException(string scope, Exception innerException = null) : base(null, innerException) {
            Scope = scope;
        }

        public virtual dynamic GetApiError() {
            return ErrorHelper.ScopeNotFoundError(Scope);
        }
    }
}
