// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.StaticContent
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
    public class StaticContentController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.StaticContentName)]
        [RequireGlobalModule(StaticContentHelper.MODULE, StaticContentHelper.DISPLAY_NAME)]
        public object Get()
        {
            Site site = ApplicationHelper.ResolveSite();
            string path = ApplicationHelper.ResolvePath();

            if (path == null) {
                return NotFound();
            }

            dynamic d = StaticContentHelper.ToJsonModel(site, path);
            return LocationChanged(StaticContentHelper.GetLocation(d.id), d);
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.StaticContentName)]
        [RequireGlobalModule(StaticContentHelper.MODULE, StaticContentHelper.DISPLAY_NAME)]
        public object Get(string id)
        {
            StaticContentId staticContentId = new StaticContentId(id);

            Site site = staticContentId.SiteId == null ? null : SiteHelper.GetSite(staticContentId.SiteId.Value);

            if (staticContentId.SiteId != null && site == null) {
                return NotFound();
            }

            return StaticContentHelper.ToJsonModel(site, staticContentId.Path);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.StaticContentName)]
        [RequireGlobalModule(StaticContentHelper.MODULE, StaticContentHelper.DISPLAY_NAME)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            StaticContentId staticContentId = new StaticContentId(id);

            Site site = staticContentId.SiteId == null ? null : SiteHelper.GetSite(staticContentId.SiteId.Value);

            if (staticContentId.SiteId != null && site == null) {
                return NotFound();
            }

            // Check for config_scope
            string configPath = model == null ? null : ManagementUnit.ResolveConfigScope(model); ;
            StaticContentSection section = StaticContentHelper.GetSection(site, staticContentId.Path, configPath);

            StaticContentHelper.UpdateFeatureSettings(model, section);

            ManagementUnit.Current.Commit();

            return StaticContentHelper.ToJsonModel(site, staticContentId.Path);
        }

        [HttpPost]
        [Audit]
        [ResourceInfo(Name = Defines.StaticContentName)]
        public async Task<object> Post()
        {
            if (StaticContentHelper.IsFeatureEnabled()) {
                throw new AlreadyExistsException(StaticContentHelper.DISPLAY_NAME);
            }

            await StaticContentHelper.SetFeatureEnabled(true);

            dynamic settings = StaticContentHelper.ToJsonModel(null, null);
            return Created(StaticContentHelper.GetLocation(settings.id), settings);
        }

        [HttpDelete]
        [Audit]
        public async Task Delete(string id)
        {
            StaticContentId staticContentId = new StaticContentId(id);

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;

            Site site = (staticContentId.SiteId != null) ? SiteHelper.GetSite(staticContentId.SiteId.Value) : null;

            if (site != null) {
                StaticContentSection section = StaticContentHelper.GetSection(site, staticContentId.Path, ManagementUnit.ResolveConfigScope());
                section.RevertToParent();
                ManagementUnit.Current.Commit();
            }

            if (staticContentId.SiteId == null && StaticContentHelper.IsFeatureEnabled()) {
                await StaticContentHelper.SetFeatureEnabled(false);
            }
        }
    }
}
