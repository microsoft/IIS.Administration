// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Authentication
{
    using Applications;
    using AspNetCore.Mvc;
    using Sites;
    using System.Net;
    using Web.Administration;
    using Core.Http;
    using Core;

    [RequireGlobalModule("AnonymousAuthenticationModule", "Anonymous Authentication")]
    public class AnonAuthController : ApiBaseController
    {
        private const string HIDDEN_FIELDS = "model.password";

        [HttpGet]
        [ResourceInfo(Name = Defines.AnonAuthenticationName)]
        public object Get()
        {
            // Check if the scope of the request is for site or application
            Site site = ApplicationHelper.ResolveSite();
            string path = ApplicationHelper.ResolvePath();

            return AnonymousAuthenticationHelper.ToJsonModel(site, path);
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.AnonAuthenticationName)]
        public object Get(string id)
        {
            AnonAuthId authId = new AnonAuthId(id);

            Site site = authId.SiteId == null ? null : SiteHelper.GetSite(authId.SiteId.Value);

            return AnonymousAuthenticationHelper.ToJsonModel(site, authId.Path);
        }

        [HttpPatch]
        [Audit(AuditAttribute.ALL, HIDDEN_FIELDS)]
        [ResourceInfo(Name = Defines.AnonAuthenticationName)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            AnonAuthId authId = new AnonAuthId(id);

            Site site = authId.SiteId == null ? null : SiteHelper.GetSite(authId.SiteId.Value);

            // Targetting section for a site, but unable to find that site
            if (authId.SiteId != null && site == null) {
                return NotFound();
            }

            string configPath = model == null ? null : ManagementUnit.ResolveConfigScope(model);
            AnonymousAuthenticationHelper.UpdateSettings(model, site, authId.Path, configPath);

            ManagementUnit.Current.Commit();

            return AnonymousAuthenticationHelper.ToJsonModel(site, authId.Path);
        }

        [HttpDelete]
        [Audit(AuditAttribute.ALL, HIDDEN_FIELDS)]
        public void Delete(string id)
        {
            AnonAuthId authId = new AnonAuthId(id);

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;

            Site site = (authId.SiteId != null) ? SiteHelper.GetSite(authId.SiteId.Value) : null;

            if (site == null) {
                return;
            }

            AnonymousAuthenticationHelper.GetSection(site, authId.Path, ManagementUnit.ResolveConfigScope()).RevertToParent();

            ManagementUnit.Current.Commit();
        }
    }
}
