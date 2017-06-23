// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.DirectoryBrowsing
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
    public class DirectoryBrowsingController : ApiBaseController
    {
        private const string DISPLAY_NAME = "Directory Browsing";

        [HttpGet]
        [ResourceInfo(Name = Defines.DirectoryBrowsingName)]
        [RequireGlobalModule(DirectoryBrowsingHelper.MODULE, DISPLAY_NAME)]
        public object Get()
        {
            Site site = ApplicationHelper.ResolveSite();
            string path = ApplicationHelper.ResolvePath();

            if (path == null) {
                return NotFound();
            }

            dynamic d = DirectoryBrowsingHelper.ToJsonModel(site, path);
            return LocationChanged(DirectoryBrowsingHelper.GetLocation(d.id), d);
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.DirectoryBrowsingName)]
        [RequireGlobalModule(DirectoryBrowsingHelper.MODULE, DISPLAY_NAME)]
        public object Get(string id)
        {
            DirectoryBrowsingId dirbId = new DirectoryBrowsingId(id);

            Site site = dirbId.SiteId == null ? null : SiteHelper.GetSite(dirbId.SiteId.Value);

            return DirectoryBrowsingHelper.ToJsonModel(site, dirbId.Path);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.DirectoryBrowsingName)]
        [RequireGlobalModule(DirectoryBrowsingHelper.MODULE, DISPLAY_NAME)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            DirectoryBrowsingId dirbId = new DirectoryBrowsingId(id);

            Site site = dirbId.SiteId == null ? null : SiteHelper.GetSite(dirbId.SiteId.Value);

            if (dirbId.SiteId != null && site == null) {
                return NotFound();
            }

            string configPath = model == null ? null : ManagementUnit.ResolveConfigScope(model);
            DirectoryBrowsingHelper.UpdateSettings(model, site, dirbId.Path, configPath);

            ManagementUnit.Current.Commit();
         
            return DirectoryBrowsingHelper.ToJsonModel(site, dirbId.Path);
        }

        [HttpPost]
        [Audit]
        [ResourceInfo(Name = Defines.DirectoryBrowsingName)]
        public async Task<object> Post()
        {
            if (DirectoryBrowsingHelper.IsFeatureEnabled()) {
                throw new AlreadyExistsException(DISPLAY_NAME);
            }

            await DirectoryBrowsingHelper.SetFeatureEnabled(true);

            dynamic settings = DirectoryBrowsingHelper.ToJsonModel(null, null);
            return Created(DirectoryBrowsingHelper.GetLocation(settings.id), settings);
        }

        [HttpDelete]
        [Audit]
        public async Task Delete(string id)
        {
            DirectoryBrowsingId dirbId = new DirectoryBrowsingId(id);

            Context.Response.StatusCode = (int) HttpStatusCode.NoContent;

            Site site = (dirbId.SiteId != null) ? SiteHelper.GetSite(dirbId.SiteId.Value) : null;

            if (site != null) {
                var section = DirectoryBrowsingHelper.GetDirectoryBrowseSection(site, dirbId.Path, ManagementUnit.ResolveConfigScope());
                section.RevertToParent();
                ManagementUnit.Current.Commit();
            }

            if (dirbId.SiteId == null && DirectoryBrowsingHelper.IsFeatureEnabled()) {
                await DirectoryBrowsingHelper.SetFeatureEnabled(false);
            }
        }
    }
}
