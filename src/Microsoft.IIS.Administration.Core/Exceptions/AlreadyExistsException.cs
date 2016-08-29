// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core {
    using System;

    public class AlreadyExistsException : ApiArgumentException {
        public AlreadyExistsException(string paramName, Exception innerException = null) : base(paramName, innerException) {
        }

        public override dynamic GetApiError() {
            return Http.ErrorHelper.AlreadyExistsError(ParamName);
        }
    }
}
