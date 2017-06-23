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
    using System.Threading.Tasks;

    [RequireWebServer]
    public class WinAuthController : ApiBaseController
    {
        private const string DISPLAY_NAME = "Windows Authentication";

        [HttpGet]
        [ResourceInfo(Name = Defines.WindowsAuthenticationName)]
        [RequireGlobalModule(WindowsAuthenticationHelper.MODULE, DISPLAY_NAME)]
        public object Get()
        {
            // Check if the scope of the request is for site or application
            Site site = ApplicationHelper.ResolveSite();
            string path = ApplicationHelper.ResolvePath();

            return WindowsAuthenticationHelper.ToJsonModel(site, path);
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.WindowsAuthenticationName)]
        [RequireGlobalModule(WindowsAuthenticationHelper.MODULE, DISPLAY_NAME)]
        public object Get(string id)
        {
            WinAuthId winAuthId = new WinAuthId(id);

            Site site = winAuthId.SiteId == null ? null : SiteHelper.GetSite(winAuthId.SiteId.Value);

            return WindowsAuthenticationHelper.ToJsonModel(site, winAuthId.Path);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.WindowsAuthenticationName)]
        [RequireGlobalModule(WindowsAuthenticationHelper.MODULE, DISPLAY_NAME)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            WinAuthId winAuthId = new WinAuthId(id);

            Site site = winAuthId.SiteId == null ? null : SiteHelper.GetSite(winAuthId.SiteId.Value);

            // Targetting section for a site, but unable to find that site
            if (winAuthId.SiteId != null && site == null) {
                return NotFound();
            }

            string configPath = model == null ? null : ManagementUnit.ResolveConfigScope(model);
            WindowsAuthenticationHelper.UpdateSettings(model, site, winAuthId.Path, configPath);

            ManagementUnit.Current.Commit();

            return WindowsAuthenticationHelper.ToJsonModel(site, winAuthId.Path);
        }

        [HttpPost]
        [Audit]
        [ResourceInfo(Name = Defines.WindowsAuthenticationName)]
        public async Task<object> Post()
        {
            if (WindowsAuthenticationHelper.IsFeatureEnabled()) {
                throw new AlreadyExistsException(WindowsAuthenticationHelper.FEATURE_NAME);
            }

            await WindowsAuthenticationHelper.SetFeatureEnabled(true);

            dynamic auth = WindowsAuthenticationHelper.ToJsonModel(null, null);
            return Created(WindowsAuthenticationHelper.GetLocation(auth.id), auth);
        }

        [HttpDelete]
        [Audit]
        public async Task Delete(string id)
        {
            WinAuthId winAuthId = new WinAuthId(id);

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;

            Site site = (winAuthId.SiteId != null) ? SiteHelper.GetSite(winAuthId.SiteId.Value) : null;

            if (site != null) {
                WindowsAuthenticationHelper.GetSection(site, winAuthId.Path, ManagementUnit.ResolveConfigScope()).RevertToParent();
                ManagementUnit.Current.Commit();
            }

            if (winAuthId.SiteId == null && WindowsAuthenticationHelper.IsFeatureEnabled()) {
                await WindowsAuthenticationHelper.SetFeatureEnabled(false);
            }
        }
    }
}
