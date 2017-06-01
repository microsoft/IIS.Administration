// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Security.Authorization {
    using Microsoft.AspNetCore.Http;
    using System.Threading.Tasks;


    interface IAuthorizationPolicy {
        Task Challenge(HttpContext ctx);
    }
}
