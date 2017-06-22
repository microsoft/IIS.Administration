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
    using System.Threading.Tasks;


    [RequireWebServer]
    public class BasicAuthController : ApiBaseController
    {
        private const string DISPLAY_NAME = "Basic Authentication";

        [HttpGet]
        [ResourceInfo(Name = Defines.BasicAuthenticationName)]
        [RequireGlobalModule(BasicAuthenticationHelper.MODULE, DISPLAY_NAME)]
        public object Get()
        {
            // Check if the scope of the request is for site or application
            Site site = ApplicationHelper.ResolveSite();
            string path = ApplicationHelper.ResolvePath();

            return BasicAuthenticationHelper.ToJsonModel(site, path);
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.BasicAuthenticationName)]
        [RequireGlobalModule(BasicAuthenticationHelper.MODULE, DISPLAY_NAME)]
        public object Get(string id)
        {
            BasicAuthId basicAuthId = new BasicAuthId(id);

            Site site = basicAuthId.SiteId == null ? null : SiteHelper.GetSite(basicAuthId.SiteId.Value);

            return BasicAuthenticationHelper.ToJsonModel(site, basicAuthId.Path);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.BasicAuthenticationName)]
        [RequireGlobalModule(BasicAuthenticationHelper.MODULE, DISPLAY_NAME)]
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

        [HttpPost]
        [Audit]
        [ResourceInfo(Name = Defines.BasicAuthenticationName)]
        public async Task<object> Post()
        {
            if (BasicAuthenticationHelper.IsFeatureEnabled()) {
                throw new AlreadyExistsException(BasicAuthenticationHelper.FEATURE_NAME);
            }

            await BasicAuthenticationHelper.SetFeatureEnabled(true);

            dynamic auth = BasicAuthenticationHelper.ToJsonModel(null, null);
            return Created(BasicAuthenticationHelper.GetLocation(auth.id), auth);
        }

        [HttpDelete]
        [Audit]
        public async Task Delete(string id)
        {
            BasicAuthId basicAuthId = new BasicAuthId(id);

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;

            Site site = (basicAuthId.SiteId != null) ? SiteHelper.GetSite(basicAuthId.SiteId.Value) : null;

            if (site != null) {
                BasicAuthenticationHelper.GetSection(site, basicAuthId.Path, ManagementUnit.ResolveConfigScope()).RevertToParent();
                ManagementUnit.Current.Commit();
            }

            if (basicAuthId.SiteId == null && BasicAuthenticationHelper.IsFeatureEnabled()) {
                await BasicAuthenticationHelper.SetFeatureEnabled(false);
            }
        }
    }
}
