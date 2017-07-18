// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer
{
    using Core;
    using System;

    public class InstallationException : Exception, IError
    {
        private int _exitCode;
        private string _productName;

        public InstallationException(int exitCode, string productName) : base()
        {
            _exitCode = exitCode;
            _productName = productName;
        }

        public dynamic GetApiError()
        {
            return ErrorHelper.InstallationError(_exitCode, _productName);
        }
    }
}
