// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Authentication
{
    using Applications;
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Sites;
    using Web.Administration;


    [RequireWebServer]
    public class AuthenticationController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.AuthenticationName)]
        public object Get()
        {
            Site site = ApplicationHelper.ResolveSite();
            string path = ApplicationHelper.ResolvePath();

            var id = new AuthenticationId(site?.Id, path);

            var obj = new {
                id = id.Uuid,
                scope = site == null ? string.Empty : site.Name + path,
                website = site == null ? null : SiteHelper.ToJsonModelRef(site)
            };

            return Environment.Hal.Apply(Defines.AuthenticationResource.Guid, obj);
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.AuthenticationName)]
        public object Get(string id)
        {
            var authId = new AuthenticationId(id);

            Site site = authId.SiteId == null ? null : SiteHelper.GetSite(authId.SiteId.Value);

            var obj = new {
                id = authId.Uuid,
                scope = site == null ? string.Empty : site.Name + authId.Path,
                website = site == null ? null : SiteHelper.ToJsonModelRef(site)
            };

            return Environment.Hal.Apply(Defines.AuthenticationResource.Guid, obj);
        }
    }
}
