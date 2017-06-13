// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core.Http {
    using AspNetCore.Authorization;


    [Authorize(Policy ="Api")]
    public abstract class ApiBaseController : ApiEdgeController {
        public virtual LocationChangedResult LocationChanged(string location, object content) {
            return new LocationChangedResult(location, content);
        }
    }
}
