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

    [RequireGlobalModule("DirectoryListingModule", "Directory Browsing")]
    public class DirectoryBrowsingController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.DirectoryBrowsingName)]
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
        public object Get(string id)
        {
            DirectoryBrowsingId dirbId = new DirectoryBrowsingId(id);

            Site site = dirbId.SiteId == null ? null : SiteHelper.GetSite(dirbId.SiteId.Value);

            return DirectoryBrowsingHelper.ToJsonModel(site, dirbId.Path);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.DirectoryBrowsingName)]
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

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            DirectoryBrowsingId dirbId = new DirectoryBrowsingId(id);

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;

            Site site = (dirbId.SiteId != null) ? SiteHelper.GetSite(dirbId.SiteId.Value) : null;

            if (site == null) {
                return;
            }

            var section = DirectoryBrowsingHelper.GetDirectoryBrowseSection(site, dirbId.Path, ManagementUnit.ResolveConfigScope());

            section.RevertToParent();

            ManagementUnit.Current.Commit();
        }
    }
}
