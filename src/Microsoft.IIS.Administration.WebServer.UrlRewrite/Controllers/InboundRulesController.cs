// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using AspNetCore.Mvc;
    using Core;
    using Core.Http;
    using Core.Utils;
    using Sites;
    using System;
    using System.Linq;
    using System.Net;
    using Web.Administration;

    public class InboundRulesController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.InboundRulesName)]
        public object Get()
        {
            string inboundRulesId = Context.Request.Query[Defines.INBOUND_RULES_SECTION_IDENTIFIER];

            if (string.IsNullOrEmpty(inboundRulesId)) {
                return NotFound();
            }

            var sectionId = new InboundRulesSectionId(inboundRulesId);

            // Get site rule is for if applicable
            Site site = sectionId.SiteId == null ? null : SiteHelper.GetSite(sectionId.SiteId.Value);

            InboundRuleCollection rules = InboundRulesHelper.GetSection(site, sectionId.Path).Rules;

            // Set HTTP header for total count
            this.Context.Response.SetItemsCount(rules.Count());

            Fields fields = Context.Request.GetFields();

            return new {
                entries = rules.Select(rule => InboundRulesHelper.RuleToJsonModelRef((InboundRuleElement)rule, site, sectionId.Path, fields))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.InboundRuleName)]
        public object Get(string id)
        {
            var inboundRuleId = new InboundRuleId(id);

            Site site = inboundRuleId.SiteId == null ? null : SiteHelper.GetSite(inboundRuleId.SiteId.Value);

            if (inboundRuleId.SiteId != null && site == null) {
                return NotFound();
            }

            InboundRuleElement rule = (InboundRuleElement)InboundRulesHelper.GetSection(site, inboundRuleId.Path).Rules.FirstOrDefault(r => r.Name.Equals(inboundRuleId.Name, StringComparison.OrdinalIgnoreCase));

            if (rule == null) {
                return NotFound();
            }

            return InboundRulesHelper.RuleToJsonModel(rule, site, inboundRuleId.Path);
        }
    }
}

