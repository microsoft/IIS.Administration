// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Authentication
{
    using Applications;
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Sites;
    using System.Net;
    using System.Threading.Tasks;
    using Web.Administration;


    [RequireWebServer]
    public class DigestAuthController : ApiBaseController
    {
        private const string DISPLAY_NAME = "Digest Authentication";

        [HttpGet]
        [ResourceInfo(Name = Defines.DigestAuthenticationName)]
        [RequireGlobalModule(DigestAuthenticationHelper.MODULE, DISPLAY_NAME)]
        public object Get()
        {
            // Check if the scope of the request is for site or application
            Site site = ApplicationHelper.ResolveSite();
            string path = ApplicationHelper.ResolvePath();

            return DigestAuthenticationHelper.ToJsonModel(site, path);
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.DigestAuthenticationName)]
        [RequireGlobalModule(DigestAuthenticationHelper.MODULE, DISPLAY_NAME)]
        public object Get(string id)
        {
            DigestAuthId digestAuthId = new DigestAuthId(id);

            Site site = digestAuthId.SiteId == null ? null : SiteHelper.GetSite(digestAuthId.SiteId.Value);

            return DigestAuthenticationHelper.ToJsonModel(site, digestAuthId.Path);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.DigestAuthenticationName)]
        [RequireGlobalModule(DigestAuthenticationHelper.MODULE, DISPLAY_NAME)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            DigestAuthId digestAuthId = new DigestAuthId(id);

            Site site = digestAuthId.SiteId == null ? null : SiteHelper.GetSite(digestAuthId.SiteId.Value);

            // Targetting section for a site, but unable to find that site
            if (digestAuthId.SiteId != null && site == null) {
                return NotFound();
            }

            string configPath = model == null ? null : ManagementUnit.ResolveConfigScope(model);
            DigestAuthenticationHelper.UpdateSettings(model, site, digestAuthId.Path, configPath);

            ManagementUnit.Current.Commit();

            return DigestAuthenticationHelper.ToJsonModel(site, digestAuthId.Path);
        }

        [HttpPost]
        [Audit]
        [ResourceInfo(Name = Defines.DigestAuthenticationName)]
        public async Task<object> Post()
        {
            if (DigestAuthenticationHelper.IsFeatureEnabled()) {
                throw new AlreadyExistsException(DigestAuthenticationHelper.FEATURE_NAME);
            }

            await DigestAuthenticationHelper.SetFeatureEnabled(true);

            dynamic auth = DigestAuthenticationHelper.ToJsonModel(null, null);
            return Created(DigestAuthenticationHelper.GetLocation(auth.id), auth);
        }

        [HttpDelete]
        [Audit]
        public async Task Delete(string id)
        {
            DigestAuthId digestAuthId = new DigestAuthId(id);

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;

            Site site = (digestAuthId.SiteId != null) ? SiteHelper.GetSite(digestAuthId.SiteId.Value) : null;

            if (site != null) {
                DigestAuthenticationHelper.GetSection(site, digestAuthId.Path, ManagementUnit.ResolveConfigScope()).RevertToParent();
                ManagementUnit.Current.Commit();
            }
            
            if (digestAuthId.SiteId == null && DigestAuthenticationHelper.IsFeatureEnabled()) {
                await DigestAuthenticationHelper.SetFeatureEnabled(false);
            }
        }
    }
}
