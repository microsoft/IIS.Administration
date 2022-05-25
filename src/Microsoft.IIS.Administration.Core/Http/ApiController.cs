// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Microsoft.IIS.Administration.Core.Http
{
    // Derived classes must provide Route attribute such as [Route("api/[controller]")]
    [ApiController]
    public abstract class ApiController : ControllerBase
    {
        /// <summary>
        /// Gets the <see cref="Microsoft.AspNetCore.Mvc.ActionContext"/>.
        /// </summary>
        public ActionContext ActionContext => ControllerContext;
        
        /// <summary>
        /// Gets the http context.
        /// </summary>
        public HttpContext Context
        {
            get
            {
                return this.HttpContext;
            }
        }

        public Uri RequestUri
        {
            get
            {
                return new Uri(Request.GetEncodedUrl());
            }
        }
    }
}
