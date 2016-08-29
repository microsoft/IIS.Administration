// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Info
{
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;

    public class WebServerInfoController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.InfoName)]
        public object Get()
        {
            return InfoHelper.ToJsonModel();
        }
    }
}
