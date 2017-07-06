// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Applications;
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
            Site site = ApplicationHelper.ResolveSite();
            string path = ApplicationHelper.ResolvePath();

            if (path == null) {
                return NotFound();
            }

            dynamic d = OutboundRulesHelper.SectionToJsonModelRef(site, path);
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
    }
}
