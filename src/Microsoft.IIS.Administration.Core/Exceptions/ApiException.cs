// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core {
    using System;


    public class ApiException : Exception, IError {

        public ApiException(string message, Exception innerException) : base(message, innerException) {
        }

        public virtual dynamic GetApiError() {
            return Http.ErrorHelper.Error(Message);
        }
    }
}
