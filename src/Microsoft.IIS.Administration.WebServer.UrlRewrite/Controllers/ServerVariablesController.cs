// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Applications;
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Sites;
    using System.Net;
    using Web.Administration;

    public class ServerVariablesController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.ServerVariablesName)]
        public object Get()
        {
            Site site = ApplicationHelper.ResolveSite();
            string path = ApplicationHelper.ResolvePath();

            if (path == null) {
                return NotFound();
            }

            dynamic d = ServerVariablesHelper.ToJsonModel(site, path);
            return LocationChanged(ServerVariablesHelper.GetLocation(d.id), d);
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.ServerVariablesName)]
        public object Get(string id)
        {
            var serverVariablesId = new ServerVariablesId(id);

            Site site = serverVariablesId.SiteId == null ? null : SiteHelper.GetSite(serverVariablesId.SiteId.Value);

            if (serverVariablesId.SiteId != null && site == null) {
                Context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }

            return ServerVariablesHelper.ToJsonModel(site, serverVariablesId.Path);
        }
    }
}

