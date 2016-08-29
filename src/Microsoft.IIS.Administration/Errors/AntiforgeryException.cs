// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration {
    using System;
    

    public class AntiforgeryException : Exception, Core.IError {

        public AntiforgeryException(Exception innerException) : base(string.Empty, innerException) {
        }

        public virtual dynamic GetApiError() {
            return ErrorHelper.AntiforgeryValidationError();
        }
    }
}
