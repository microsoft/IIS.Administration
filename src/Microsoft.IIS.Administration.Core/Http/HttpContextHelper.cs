// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.Core.Http
{
    using AspNetCore.Http;
    using System;


    public static class HttpHelper
    {
        static IHttpContextAccessor _contextAccessor;

        public static HttpContext Current
        {
            get
            {
                return _contextAccessor.HttpContext;
            }
        }

        public static IHttpContextAccessor HttpContextAccessor
        {
            set
            {
                if (value == null) {
                    throw new ArgumentNullException("HttpContextAccessor");
                }

                _contextAccessor = value;
            }
        }
    }
}
