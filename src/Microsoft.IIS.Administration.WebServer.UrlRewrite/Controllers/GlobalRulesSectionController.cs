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
    public class GlobalRulesSectionController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.GlobalRulesSectionName)]
        public object Get()
        {
            RewriteHelper.ResolveRewrite(Context, out Site site, out string path);

            if (path == null) {
                return NotFound();
            }

            dynamic d = GlobalRulesHelper.SectionToJsonModel(site, path);
            return LocationChanged(GlobalRulesHelper.GetSectionLocation(d.id), d);
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.GlobalRulesSectionName)]
        public object Get(string id)
        {
            var rewriteId = new RewriteId(id);

            Site site = rewriteId.SiteId == null ? null : SiteHelper.GetSite(rewriteId.SiteId.Value);

            if (rewriteId.SiteId != null && site == null) {
                Context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }

            return GlobalRulesHelper.SectionToJsonModel(site, rewriteId.Path);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.GlobalRulesSectionName)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            RewriteId globalRulesId = new RewriteId(id);

            Site site = globalRulesId.SiteId == null ? null : SiteHelper.GetSite(globalRulesId.SiteId.Value);

            if (globalRulesId.SiteId != null && site == null) {
                return NotFound();
            }

            string configPath = model == null ? null : ManagementUnit.ResolveConfigScope(model);

            GlobalRulesHelper.UpdateSection(model, site, globalRulesId.Path, configPath);

            ManagementUnit.Current.Commit();

            return GlobalRulesHelper.SectionToJsonModel(site, globalRulesId.Path);
        }

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            var globalRulesId = new RewriteId(id);

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;

            Site site = (globalRulesId.SiteId != null) ? SiteHelper.GetSite(globalRulesId.SiteId.Value) : null;

            if (site != null) {
                var section = GlobalRulesHelper.GetSection(site, globalRulesId.Path, ManagementUnit.ResolveConfigScope());
                section.RevertToParent();
                ManagementUnit.Current.Commit();
            }
        }
    }
}
