// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core
{
    using System;

    public class ApiNotAllowedException : Exception, IError {
        private string _message;

        public override string Message {
            get {
                return _message;
            }
        }

        public ApiNotAllowedException(string message, Exception innerException = null) : base(message, innerException) {
            this._message = message;
        }

        public virtual dynamic GetApiError() {
            return Http.ErrorHelper.NotAllowedError(Message);
        }
    }
}
