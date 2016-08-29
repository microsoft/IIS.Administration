// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Authentication
{
    using AspNetCore.Mvc;
    using Applications;
    using Sites;
    using Web.Administration;
    using System.Net;
    using Core.Http;
    using Core;

    [RequireGlobalModule("BasicAuthenticationModule", "Basic Authentication")]
    public class BasicAuthController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.BasicAuthenticationName)]
        public object Get()
        {
            // Check if the scope of the request is for site or application
            Site site = ApplicationHelper.ResolveSite();
            string path = ApplicationHelper.ResolvePath();

            return BasicAuthenticationHelper.ToJsonModel(site, path);
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.BasicAuthenticationName)]
        public object Get(string id)
        {
            BasicAuthId basicAuthId = new BasicAuthId(id);

            Site site = basicAuthId.SiteId == null ? null : SiteHelper.GetSite(basicAuthId.SiteId.Value);

            return BasicAuthenticationHelper.ToJsonModel(site, basicAuthId.Path);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.BasicAuthenticationName)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            BasicAuthId basicAuthId = new BasicAuthId(id);

            Site site = basicAuthId.SiteId == null ? null : SiteHelper.GetSite(basicAuthId.SiteId.Value);

            // Targetting section for a site, but unable to find that site
            if (basicAuthId.SiteId != null && site == null) {
                return NotFound();
            }

            string configPath = model == null ? null : ManagementUnit.ResolveConfigScope(model);
            BasicAuthenticationHelper.UpdateSettings(model, site, basicAuthId.Path, configPath);

            ManagementUnit.Current.Commit();

            return BasicAuthenticationHelper.ToJsonModel(site, basicAuthId.Path);
        }

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            BasicAuthId basicAuthId = new BasicAuthId(id);

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;

            Site site = (basicAuthId.SiteId != null) ? SiteHelper.GetSite(basicAuthId.SiteId.Value) : null;

            if (site == null) {
                return;
            }

            BasicAuthenticationHelper.GetSection(site, basicAuthId.Path, ManagementUnit.ResolveConfigScope()).RevertToParent();

            ManagementUnit.Current.Commit();
        }
    }
}
