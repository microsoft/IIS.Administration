// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer {
    using System;
    using Core;


    public class FeatureNotFoundException : NotFoundException {

        public FeatureNotFoundException(string name, Exception innerException = null) : base(name, innerException) {
        }

        public override dynamic GetApiError() {
            return ErrorHelper.FeatureNotFoundError(ParamName);
        }
    }
}
