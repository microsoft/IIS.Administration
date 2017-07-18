// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.IIS.Administration.Core;
    using Microsoft.IIS.Administration.Core.Http;
    using Microsoft.IIS.Administration.WebServer.Sites;
    using Microsoft.Web.Administration;
    using System.Net;

    [RequireGlobalModule(RewriteHelper.MODULE, RewriteHelper.DISPLAY_NAME)]
    public class RewriteMapsSectionController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.RewriteMapsSectionName)]
        public object Get()
        {
            RewriteHelper.ResolveRewrite(Context, out Site site, out string path);

            if (path == null) {
                return NotFound();
            }

            dynamic d = RewriteMapsHelper.SectionToJsonModel(site, path);
            return LocationChanged(RewriteMapsHelper.GetSectionLocation(d.id), d);
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.RewriteMapsSectionName)]
        public object Get(string id)
        {
            var rewriteId = new RewriteId(id);

            Site site = rewriteId.SiteId == null ? null : SiteHelper.GetSite(rewriteId.SiteId.Value);

            if (rewriteId.SiteId != null && site == null) {
                Context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }

            return RewriteMapsHelper.SectionToJsonModel(site, rewriteId.Path);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.RewriteMapsSectionName)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            var rewriteMapsId = new RewriteId(id);

            Site site = rewriteMapsId.SiteId == null ? null : SiteHelper.GetSite(rewriteMapsId.SiteId.Value);

            if (rewriteMapsId.SiteId != null && site == null) {
                return NotFound();
            }

            string configPath = model == null ? null : ManagementUnit.ResolveConfigScope(model);

            RewriteMapsHelper.UpdateSection(model, site, rewriteMapsId.Path, configPath);

            ManagementUnit.Current.Commit();

            return RewriteMapsHelper.SectionToJsonModel(site, rewriteMapsId.Path);
        }

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            var rewriteMapsId = new RewriteId(id);

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;

            Site site = (rewriteMapsId.SiteId != null) ? SiteHelper.GetSite(rewriteMapsId.SiteId.Value) : null;

            if (site != null) {
                var section = RewriteMapsHelper.GetSection(site, rewriteMapsId.Path, ManagementUnit.ResolveConfigScope());
                section.RevertToParent();
                ManagementUnit.Current.Commit();
            }
        }
    }
}
