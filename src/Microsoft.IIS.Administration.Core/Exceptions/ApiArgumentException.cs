// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core {
    using System;


    public class ApiArgumentException : ArgumentException, IError {
        public const string EXPECTED_ARRAY = "Expected array";
        public const string EXPECTED_OBJECT = "Expected object";

        private string _message;

        public override string Message {
            get {
                return _message;
            }
        }

        public ApiArgumentException(string paramName, Exception innerException = null) : base(null, paramName, innerException) {
            this._message = null;
        }

        public ApiArgumentException(string paramName, string message, Exception innerException = null) : base(message, paramName, innerException) {
            this._message = message;
        }

        public virtual dynamic GetApiError() {
            return Http.ErrorHelper.ArgumentError(ParamName, Message);
        }
    }

    public class ApiArgumentOutOfRangeException : ApiArgumentException {

        private long _min;
        private long _max;

        public ApiArgumentOutOfRangeException(string paramName, long min, long max, Exception innerException = null) 
            : base (paramName, $"Value must be between {min} and {max} inclusive.", innerException) {
            _min = min;
            _max = max;
        }

        public ApiArgumentOutOfRangeException(string paramName, long min, long max, string message, Exception innerException = null)
            : base(paramName, message, innerException)
        {
            _min = min;
            _max = max;
        }

        public override dynamic GetApiError() {
            return Http.ErrorHelper.ArgumentOutOfRangeError(ParamName, _min, _max, Message);
        }
    }
}
