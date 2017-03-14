// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer
{
    using Core;
    using System;

    public class DismException : Exception, IError
    {
        private int _exitCode;
        private string _featureName;

        public DismException(int exitCode, string featureName) : base()
        {
            _exitCode = exitCode;
            _featureName = featureName;
        }

        public dynamic GetApiError()
        {
            return ErrorHelper.DismError(_exitCode, _featureName);
        }
    }
}
