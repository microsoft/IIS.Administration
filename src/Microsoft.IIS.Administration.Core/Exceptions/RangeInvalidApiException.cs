// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core
{
    using System;
    using System.Dynamic;
    using System.Net;

    public class InvalidRangeException : Exception, IError
    {
        public InvalidRangeException(Exception innerException = null) : base(string.Empty, innerException) { }

        public InvalidRangeException(string message, Exception innerException = null) : base(message == null ? string.Empty : message, innerException) { }

        public dynamic GetApiError()
        {
            dynamic obj = new ExpandoObject();
            obj.title = "Invalid Range";

            if (!string.IsNullOrEmpty(Message)) {
                obj.detail = Message;
            }

            obj.status = (int)HttpStatusCode.RequestedRangeNotSatisfiable;
            return obj;
        }
    }
}
