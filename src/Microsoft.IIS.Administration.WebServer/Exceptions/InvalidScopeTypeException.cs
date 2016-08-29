// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer {
    using System;
    using Core;


    public class InvalidScopeTypeException : Exception, IError {

        public string Scope { get; private set; }

        public InvalidScopeTypeException(string scope, Exception innerException = null) : base(scope, innerException) {
            Scope = scope;
        }

        public dynamic GetApiError() {
            return ErrorHelper.InvalidScopeTypeError(Scope);
        }
    }
}
