// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.UrlRewrite
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.IIS.Administration.Core;
    using Microsoft.IIS.Administration.Core.Http;
    using Microsoft.IIS.Administration.WebServer.Sites;
    using Microsoft.Web.Administration;
    using System;
    using System.Linq;
    using System.Net;

    [RequireGlobalModule(RewriteHelper.MODULE, RewriteHelper.DISPLAY_NAME)]
    public class GlobalRulesController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.GlobalRulesName)]
        public object Get()
        {
            string globalRulesId = Context.Request.Query[Defines.IDENTIFIER];

            if (string.IsNullOrEmpty(globalRulesId)) {
                return NotFound();
            }

            var sectionId = new RewriteId(globalRulesId);

            Site site = sectionId.SiteId == null ? null : SiteHelper.GetSite(sectionId.SiteId.Value);

            InboundRuleCollection rules = GlobalRulesHelper.GetSection(site, sectionId.Path).InboundRules;

            this.Context.Response.SetItemsCount(rules.Count());

            return new {
                rules = rules.Select(rule => GlobalRulesHelper.RuleToJsonModelRef((InboundRule)rule, site, sectionId.Path, Context.Request.GetFields()))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.GlobalRuleName)]
        public object Get(string id)
        {
            var inboundRuleId = new InboundRuleId(id);

            Site site = inboundRuleId.SiteId == null ? null : SiteHelper.GetSite(inboundRuleId.SiteId.Value);

            if (inboundRuleId.SiteId != null && site == null) {
                return NotFound();
            }

            InboundRule rule = (InboundRule)GlobalRulesHelper.GetSection(site, inboundRuleId.Path).InboundRules.FirstOrDefault(r => r.Name.Equals(inboundRuleId.Name, StringComparison.OrdinalIgnoreCase));

            if (rule == null) {
                return NotFound();
            }

            return GlobalRulesHelper.RuleToJsonModel(rule, site, inboundRuleId.Path, Context.Request.GetFields());
        }

        [HttpPatch]
        [ResourceInfo(Name = Defines.GlobalRuleName)]
        [Audit]
        public object Patch([FromBody]dynamic model, string id)
        {
            var globalRuleId = new InboundRuleId(id);

            Site site = globalRuleId.SiteId == null ? null : SiteHelper.GetSite(globalRuleId.SiteId.Value);

            if (globalRuleId.SiteId != null && site == null) {
                return NotFound();
            }

            InboundRulesSection section = GlobalRulesHelper.GetSection(site, globalRuleId.Path);
            InboundRule rule = (InboundRule)section.InboundRules.FirstOrDefault(r => r.Name.Equals(globalRuleId.Name, StringComparison.OrdinalIgnoreCase));

            if (rule == null) {
                return NotFound();
            }

            GlobalRulesHelper.UpdateRule(model, rule, section);

            ManagementUnit.Current.Commit();

            dynamic updatedRule = GlobalRulesHelper.RuleToJsonModel(rule, site, globalRuleId.Path, Context.Request.GetFields(), true);

            if (updatedRule.id != id) {
                return LocationChanged(GlobalRulesHelper.GetRuleLocation(updatedRule.id), updatedRule);
            }

            return updatedRule;
        }

        [HttpPost]
        [ResourceInfo(Name = Defines.GlobalRuleName)]
        [Audit]
        public object Post([FromBody]dynamic model)
        {
            if (model == null) {
                throw new ApiArgumentException("model");
            }

            RewriteId parentId = RewriteHelper.GetRewriteIdFromBody(model);

            if (parentId == null) {
                throw new ApiArgumentException("url_rewrite");
            }

            Site site = parentId.SiteId == null ? null : SiteHelper.GetSite(parentId.SiteId.Value);

            string configPath = ManagementUnit.ResolveConfigScope(model);
            InboundRulesSection section = GlobalRulesHelper.GetSection(site, parentId.Path, configPath);

            InboundRule rule = GlobalRulesHelper.CreateRule(model, section);

            GlobalRulesHelper.AddRule(rule, section, model);

            ManagementUnit.Current.Commit();

            dynamic r = GlobalRulesHelper.RuleToJsonModel(rule, site, parentId.Path, Context.Request.GetFields(), true);
            return Created(GlobalRulesHelper.GetRuleLocation(r.id), r);
        }

        [HttpDelete]
        public void Delete(string id)
        {
            InboundRule rule = null;
            var globalRuleId = new InboundRuleId(id);

            Site site = globalRuleId.SiteId == null ? null : SiteHelper.GetSite(globalRuleId.SiteId.Value);

            if (globalRuleId.SiteId == null || site != null) {
                rule = (InboundRule)GlobalRulesHelper.GetSection(site, globalRuleId.Path).InboundRules.FirstOrDefault(r => r.Name.Equals(globalRuleId.Name, StringComparison.OrdinalIgnoreCase));
            }

            if (rule != null) {
                var section = GlobalRulesHelper.GetSection(site, globalRuleId.Path, ManagementUnit.ResolveConfigScope());

                GlobalRulesHelper.DeleteRule(rule, section);
                ManagementUnit.Current.Commit();
            }

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }
    }
}
