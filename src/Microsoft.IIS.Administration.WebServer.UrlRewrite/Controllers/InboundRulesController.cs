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

    [RequireGlobalModule(RewriteHelper.MODULE, RewriteHelper.DISPLAY_NAME)]
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

            if (string.IsNullOrEmpty(inboundRulesId)) {
                return NotFound();
            }

            var sectionId = new RewriteId(inboundRulesId);

            // Get site rule is for if applicable
            Site site = sectionId.SiteId == null ? null : SiteHelper.GetSite(sectionId.SiteId.Value);

            InboundRuleCollection rules = InboundRulesHelper.GetSection(site, sectionId.Path).InboundRules;

            // Set HTTP header for total count
            this.Context.Response.SetItemsCount(rules.Count());

            Fields fields = Context.Request.GetFields();

            return new {
                entries = rules.Select(rule => InboundRulesHelper.RuleToJsonModelRef((InboundRule)rule, site, sectionId.Path, fields))
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

            InboundRule rule = (InboundRule)InboundRulesHelper.GetSection(site, inboundRuleId.Path).InboundRules.FirstOrDefault(r => r.Name.Equals(inboundRuleId.Name, StringComparison.OrdinalIgnoreCase));

            if (rule == null) {
                return NotFound();
            }

            return InboundRulesHelper.RuleToJsonModel(rule, site, inboundRuleId.Path);
        }

        [HttpPatch]
        [ResourceInfo(Name = Defines.InboundRuleName)]
        [Audit]
        public object Patch([FromBody]dynamic model, string id)
        {
            var inboundRuleId = new InboundRuleId(id);

            Site site = inboundRuleId.SiteId == null ? null : SiteHelper.GetSite(inboundRuleId.SiteId.Value);

            if (inboundRuleId.SiteId != null && site == null) {
                return NotFound();
            }

            InboundRulesSection section = InboundRulesHelper.GetSection(site, inboundRuleId.Path);
            InboundRule rule = (InboundRule)section.InboundRules.FirstOrDefault(r => r.Name.Equals(inboundRuleId.Name, StringComparison.OrdinalIgnoreCase));

            if (rule == null) {
                return NotFound();
            }

            InboundRulesHelper.UpdateRule(model, rule, section);

            ManagementUnit.Current.Commit();

            dynamic updatedRule = InboundRulesHelper.RuleToJsonModel(rule, site, inboundRuleId.Path, null, true);

            if (updatedRule.id != id) {
                return LocationChanged(InboundRulesHelper.GetRuleLocation(updatedRule.id), updatedRule);
            }

            return updatedRule;
        }

        [HttpPost]
        [ResourceInfo(Name = Defines.InboundRuleName)]
        [Audit]
        public object Post([FromBody]dynamic model)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            RewriteId parentId = RewriteHelper.GetRewriteIdFromBody(model) ?? InboundRulesHelper.GetSectionIdFromBody(model);

            // Get site the rule is for if applicable
            Site site = parentId.SiteId == null ? null : SiteHelper.GetSite(parentId.SiteId.Value);

            string configPath = ManagementUnit.ResolveConfigScope(model);
            InboundRulesSection section = InboundRulesHelper.GetSection(site, parentId.Path, configPath);

            // Create rule
            InboundRule rule = InboundRulesHelper.CreateRule(model, section);

            // Add it
            InboundRulesHelper.AddRule(rule, section);

            // Save
            ManagementUnit.Current.Commit();

            //
            // Create response
            dynamic r = InboundRulesHelper.RuleToJsonModel(rule, site, parentId.Path, null, true);
            return Created(InboundRulesHelper.GetRuleLocation(r.id), r);
        }

        [HttpDelete]
        public void Delete(string id)
        {
            InboundRule rule = null;
            var inboundRuleId = new InboundRuleId(id);

            Site site = inboundRuleId.SiteId == null ? null : SiteHelper.GetSite(inboundRuleId.SiteId.Value);

            if (inboundRuleId.SiteId == null || site != null) {
                rule = (InboundRule)InboundRulesHelper.GetSection(site, inboundRuleId.Path).InboundRules.FirstOrDefault(r => r.Name.Equals(inboundRuleId.Name, StringComparison.OrdinalIgnoreCase));
            }

            if (rule != null) {
                var section = InboundRulesHelper.GetSection(site, inboundRuleId.Path, ManagementUnit.ResolveConfigScope());

                InboundRulesHelper.DeleteRule(rule, section);
                ManagementUnit.Current.Commit();
            }

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }
    }
}

