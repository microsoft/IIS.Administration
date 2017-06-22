// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.HttpRedirect
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
    public class HttpRedirectController : ApiBaseController
    {
        private const string DISPLAY_NAME = "HTTP Redirect";

        [HttpGet]
        [ResourceInfo(Name = Defines.HttpRedirectName)]
        [RequireGlobalModule(RedirectHelper.MODULE, DISPLAY_NAME)]
        public object Get()
        {
            Site site = ApplicationHelper.ResolveSite();
            string path = ApplicationHelper.ResolvePath();

            if (path == null) {
                return NotFound();
            }

            dynamic d = RedirectHelper.ToJsonModel(site, path);
            return LocationChanged(RedirectHelper.GetLocation(d.id), d);
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.HttpRedirectName)]
        [RequireGlobalModule(RedirectHelper.MODULE, DISPLAY_NAME)]
        public object Get(string id)
        {
            var redId = new RedirectId(id);

            Site site = redId.SiteId == null ? null : SiteHelper.GetSite(redId.SiteId.Value);

            return RedirectHelper.ToJsonModel(site, redId.Path);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.HttpRedirectName)]
        [RequireGlobalModule(RedirectHelper.MODULE, DISPLAY_NAME)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            
            var redId = new RedirectId(id);

            Site site = redId.SiteId == null ? null : SiteHelper.GetSite(redId.SiteId.Value);

            if (redId.SiteId != null && site == null) {
                return NotFound();
            }

            string configPath = model == null ? null : ManagementUnit.ResolveConfigScope(model);
            HttpRedirectSection section = RedirectHelper.GetRedirectSection(site, redId.Path, configPath);

            RedirectHelper.UpdateFeatureSettings(model, section);

            ManagementUnit.Current.Commit();

            return RedirectHelper.ToJsonModel(site, redId.Path);
        }

        [HttpPost]
        [Audit]
        [ResourceInfo(Name = Defines.HttpRedirectName)]
        public async Task<object> Post()
        {
            if (RedirectHelper.IsFeatureEnabled()) {
                throw new AlreadyExistsException(RedirectHelper.FEATURE);
            }

            await RedirectHelper.SetFeatureEnabled(true);

            dynamic settings = RedirectHelper.ToJsonModel(null, null);
            return Created(RedirectHelper.GetLocation(settings.id), settings);
        }

        [HttpDelete]
        [Audit]
        public async Task Delete(string id)
        {
            var redId = new RedirectId(id);

            Context.Response.StatusCode = (int) HttpStatusCode.NoContent;

            Site site = (redId.SiteId != null) ? SiteHelper.GetSite(redId.SiteId.Value) : null;

            if (site != null) {
                var section = RedirectHelper.GetRedirectSection(site, redId.Path, ManagementUnit.ResolveConfigScope());
                section.RevertToParent();
                ManagementUnit.Current.Commit();
            }
            
            if (redId.SiteId == null && RedirectHelper.IsFeatureEnabled()) {
                await RedirectHelper.SetFeatureEnabled(false);
            }
        }
    }
}
