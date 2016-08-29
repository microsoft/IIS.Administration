// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer
{
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;

    [ResourceInfo(Name=Defines.ResourceName)]
    public class WebServerController : ApiBaseController
    {
        [HttpGet]
        public object Get()
        {
            return WebServerHelper.WebServerJsonModel();
        }

        [HttpGet]
        public object Get(string id)
        {
            if(id != WebServerId.CreateFromPath(ManagementUnit.Current.ApplicationHostConfigPath).Uuid)
            {
                return NotFound();
            }

            return WebServerHelper.WebServerJsonModel();
        }
    }
}
