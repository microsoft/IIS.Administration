// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Files
{
    using Core;
    using System;
    using System.Dynamic;
    using System.Net;

    public class ForbiddenPathException : Exception, IError
    {
        private string _path;

        public ForbiddenPathException(string path, Exception innerException = null) : base(string.Empty, innerException) {
            this._path = path;
        }

        public ForbiddenPathException(string path, string message, Exception innerException = null) : base(message == null ? string.Empty : message, innerException) {
            this._path = path;
        }

        public dynamic GetApiError()
        {
            dynamic obj = new ExpandoObject();
            obj.title = "Path Forbidden";
            obj.name = _path;

            if (!string.IsNullOrEmpty(Message)) {
                obj.detail = Message;
            }

            obj.status = (int)HttpStatusCode.Forbidden;
            return obj;
        }
    }
}
