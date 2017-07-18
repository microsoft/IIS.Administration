// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Sites;
    using System.Net;
    using Web.Administration;

    [RequireGlobalModule(RewriteHelper.MODULE, RewriteHelper.DISPLAY_NAME)]
    public class InboundRulesSectionController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.InboundRulesSectionName)]
        public object Get()
        {
            RewriteHelper.ResolveRewrite(Context, out Site site, out string path);

            if (path == null) {
                return NotFound();
            }

            dynamic d = InboundRulesHelper.SectionToJsonModel(site, path);
            return LocationChanged(InboundRulesHelper.GetSectionLocation(d.id), d);
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.InboundRulesSectionName)]
        public object Get(string id)
        {
            var rewriteId = new RewriteId(id);

            Site site = rewriteId.SiteId == null ? null : SiteHelper.GetSite(rewriteId.SiteId.Value);

            if (rewriteId.SiteId != null && site == null) {
                Context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }

            return InboundRulesHelper.SectionToJsonModel(site, rewriteId.Path);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.InboundRulesSectionName)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            RewriteId inboundRulesId = new RewriteId(id);

            Site site = inboundRulesId.SiteId == null ? null : SiteHelper.GetSite(inboundRulesId.SiteId.Value);

            if (inboundRulesId.SiteId != null && site == null) {
                return NotFound();
            }

            string configPath = model == null ? null : ManagementUnit.ResolveConfigScope(model);

            InboundRulesHelper.UpdateSection(model, site, inboundRulesId.Path, configPath);

            ManagementUnit.Current.Commit();

            return InboundRulesHelper.SectionToJsonModel(site, inboundRulesId.Path);
        }

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            var inboundRulesId = new RewriteId(id);

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;

            Site site = (inboundRulesId.SiteId != null) ? SiteHelper.GetSite(inboundRulesId.SiteId.Value) : null;

            if (site != null) {
                var section = InboundRulesHelper.GetSection(site, inboundRulesId.Path, ManagementUnit.ResolveConfigScope());
                section.RevertToParent();
                ManagementUnit.Current.Commit();
            }
        }
    }
}

