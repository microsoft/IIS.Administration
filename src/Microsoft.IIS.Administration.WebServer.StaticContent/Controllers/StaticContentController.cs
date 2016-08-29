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

    [RequireGlobalModule("StaticFileModule", "Static Content")]
    public class StaticContentController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.StaticContentName)]
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

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            StaticContentId staticContentId = new StaticContentId(id);

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;

            Site site = (staticContentId.SiteId != null) ? SiteHelper.GetSite(staticContentId.SiteId.Value) : null;

            if (site == null) {
                return;
            }

            StaticContentSection section = StaticContentHelper.GetSection(site, staticContentId.Path, ManagementUnit.ResolveConfigScope());

            section.RevertToParent();

            ManagementUnit.Current.Commit();
        }
    }
}
