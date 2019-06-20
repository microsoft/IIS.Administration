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
        private string _errors;
        private string _outputs;

        public DismException(int exitCode, string featureName, string errors, string outputs) : base()
        {
            _exitCode = exitCode;
            _featureName = featureName;
            _errors = errors;
            _outputs = outputs;
        }

        public dynamic GetApiError()
        {
            return ErrorHelper.DismError(_exitCode, _featureName, _errors, _outputs);
        }
    }
}
