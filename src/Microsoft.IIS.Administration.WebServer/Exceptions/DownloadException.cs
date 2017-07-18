// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer
{
    using Core;
    using System;

    public class DownloadException : Exception, IError
    {
        private string _name;

        public DownloadException(string name) : base()
        {
            _name = name;
        }

        public dynamic GetApiError()
        {
            return ErrorHelper.DownloadError(_name);
        }
    }
}
