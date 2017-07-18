// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Core;
    using Core.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Web.Administration;
    using Sites;
    using System.Net;

    public class OutboundRulesSectionController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.OutboundRulesSectionName)]
        public object Get()
        {
            RewriteHelper.ResolveRewrite(Context, out Site site, out string path);

            if (path == null) {
                return NotFound();
            }

            dynamic d = OutboundRulesHelper.SectionToJsonModel(site, path);
            return LocationChanged(OutboundRulesHelper.GetSectionLocation(d.id), d);
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.OutboundRulesSectionName)]
        public object Get(string id)
        {
            var rewriteId = new RewriteId(id);

            Site site = rewriteId.SiteId == null ? null : SiteHelper.GetSite(rewriteId.SiteId.Value);

            if (rewriteId.SiteId != null && site == null) {
                Context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return null;
            }

            return OutboundRulesHelper.SectionToJsonModel(site, rewriteId.Path);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.OutboundRulesSectionName)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            var outboundRulesId = new RewriteId(id);

            Site site = outboundRulesId.SiteId == null ? null : SiteHelper.GetSite(outboundRulesId.SiteId.Value);

            if (outboundRulesId.SiteId != null && site == null) {
                return NotFound();
            }

            string configPath = model == null ? null : ManagementUnit.ResolveConfigScope(model);

            OutboundRulesHelper.UpdateSection(model, site, outboundRulesId.Path, configPath);

            ManagementUnit.Current.Commit();

            return OutboundRulesHelper.SectionToJsonModel(site, outboundRulesId.Path);
        }

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            var outboundRulesId = new RewriteId(id);

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;

            Site site = (outboundRulesId.SiteId != null) ? SiteHelper.GetSite(outboundRulesId.SiteId.Value) : null;

            if (site != null) {
                var section = OutboundRulesHelper.GetSection(site, outboundRulesId.Path, ManagementUnit.ResolveConfigScope());
                section.RevertToParent();
                ManagementUnit.Current.Commit();
            }
        }
    }
}
