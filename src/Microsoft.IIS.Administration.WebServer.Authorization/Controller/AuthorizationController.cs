// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.Authorization
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
    public class AuthorizationController : ApiBaseController
    {
        private const string DISPLAY_NAME = "Authorization";

        [HttpGet]
        [ResourceInfo(Name = Defines.AuthorizationName)]
        [RequireGlobalModule(AuthorizationHelper.MODULE, DISPLAY_NAME)]
        public object Get()
        {
            Site site = ApplicationHelper.ResolveSite();
            string path = ApplicationHelper.ResolvePath();

            return AuthorizationHelper.ToJsonModel(site, path);
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.AuthorizationName)]
        [RequireGlobalModule(AuthorizationHelper.MODULE, DISPLAY_NAME)]
        public object Get(string id)
        {
            AuthorizationId authId = new AuthorizationId(id);

            Site site = authId.SiteId == null ? null : SiteHelper.GetSite(authId.SiteId.Value);

            return AuthorizationHelper.ToJsonModel(site, authId.Path);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.AuthorizationName)]
        [RequireGlobalModule(AuthorizationHelper.MODULE, DISPLAY_NAME)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            AuthorizationId authId = new AuthorizationId(id);

            Site site = authId.SiteId == null ? null : SiteHelper.GetSite(authId.SiteId.Value);

            if (authId.SiteId != null && site == null) {
                return NotFound();
            }

            // Check for config_scope
            string configPath = model == null ? null : ManagementUnit.ResolveConfigScope(model);
            var section = AuthorizationHelper.GetSection(site, authId.Path, configPath);

            AuthorizationHelper.UpdateFeatureSettings(model, section);

            ManagementUnit.Current.Commit();

            dynamic authorization = AuthorizationHelper.ToJsonModel(site, authId.Path);

            if(authorization.id != id) {
                return LocationChanged(AuthorizationHelper.GetLocation(authorization.id), authorization);
            }

            return authorization;
        }

        [HttpPost]
        [Audit]
        [ResourceInfo(Name = Defines.AuthorizationName)]
        public async Task<object> Post()
        {
            if (AuthorizationHelper.IsFeatureEnabled()) {
                throw new AlreadyExistsException(AuthorizationHelper.FEATURE_NAME);
            }

            await AuthorizationHelper.SetFeatureEnabled(true);

            dynamic auth = AuthorizationHelper.ToJsonModel(null, null);
            return Created(AuthorizationHelper.GetLocation(auth.id), auth);
        }

        [HttpDelete]
        [Audit]
        public async Task Delete(string id)
        {
            AuthorizationId authId = new AuthorizationId(id);

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;

            Site site = (authId.SiteId != null) ? SiteHelper.GetSite(authId.SiteId.Value) : null;

            if (site != null) {
                var section = AuthorizationHelper.GetSection(site, authId.Path, ManagementUnit.ResolveConfigScope());
                section.RevertToParent();
                ManagementUnit.Current.Commit();
            }

            if (authId.SiteId == null && AuthorizationHelper.IsFeatureEnabled()) {
                await AuthorizationHelper.SetFeatureEnabled(false);
            }
        }
    }
}
