// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core {
    using System;


    public class UnauthorizedArgumentException : ApiArgumentException {

        public UnauthorizedArgumentException(string paramName, Exception innerException = null) : base(paramName, innerException) {
        }

        public UnauthorizedArgumentException(string paramName, string message, Exception innerException = null) : base(paramName, message == null ? string.Empty : message, innerException)
        {
        }

        public UnauthorizedArgumentException(string paramName, string message, string value, Exception innerException = null) : base(paramName, message == null ? string.Empty : message, innerException)
        {
            Value = value;
        }

        public string Value { get; private set; }


        public override dynamic GetApiError() {
            return Http.ErrorHelper.UnauthorizedArgumentError(ParamName, Message, Value);
        }
    }
}
