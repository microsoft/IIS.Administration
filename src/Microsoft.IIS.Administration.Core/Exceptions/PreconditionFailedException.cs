// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core
{
    using System;
    using System.Dynamic;
    using System.Net;

    public class PreconditionFailedException : Exception, IError
    {
        private string _message;
        private string _precondition;

        public PreconditionFailedException(string precondition, Exception innerException = null) : base(null, innerException) {
            _precondition = precondition;
        }

        public PreconditionFailedException(string precondition, string message, Exception innerException = null) : base(message, innerException)
        {
            _precondition = precondition;
            _message = message;
        }

        public override string Message
        {
            get {
                return _message;
            }
        }

        public dynamic GetApiError()
        {
            dynamic obj = new ExpandoObject();
            obj.title = "Precondition Failed";
            obj.name = _precondition;

            if (!string.IsNullOrEmpty(Message)) {
                obj.detail = Message;
            }

            obj.status = (int)HttpStatusCode.PreconditionFailed;
            return obj;
        }
    }
}
