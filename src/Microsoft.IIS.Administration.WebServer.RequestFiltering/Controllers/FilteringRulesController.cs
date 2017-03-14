// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.IIS.Administration.WebServer.RequestFiltering
{
    using AspNetCore.Mvc;
    using Core;
    using Web.Administration;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Sites;
    using Applications;
    using System.Net;
    using Newtonsoft.Json.Linq;
    using Core.Utils;
    using Core.Http;

    [RequireGlobalModule(RequestFilteringHelper.MODULE, RequestFilteringHelper.DISPLAY_NAME)]
    public class FilteringRulesController : ApiBaseController
    {
        [HttpGet]
        [ResourceInfo(Name = Defines.FilteringRulesName)]
        public object Get()
        {
            string requestFilteringUuid = Context.Request.Query[Defines.IDENTIFIER];

            if (string.IsNullOrEmpty(requestFilteringUuid)) {
                return new StatusCodeResult((int)HttpStatusCode.NotFound);
            }

            RequestFilteringId reqId = new RequestFilteringId(requestFilteringUuid);

            // Get site and application rule is for if applicable
            Site site = reqId.SiteId == null ? null : SiteHelper.GetSite(reqId.SiteId.Value);

            List<Rule> rules = RulesHelper.GetRules(site, reqId.Path);

            Fields fields = Context.Request.GetFields();

            return new {
                rules = rules.Select(r => RulesHelper.ToJsonModelRef(r, site, reqId.Path, fields))
            };
        }

        [HttpGet]
        [ResourceInfo(Name = Defines.FilteringRuleName)]
        public object Get(string id)
        {
            RuleId ruleId = new RuleId(id);

            Site site = ruleId.SiteId == null ? null : SiteHelper.GetSite(ruleId.SiteId.Value);

            if (ruleId.SiteId != null && site == null) {
                // The rule id specified a site but we couldn't find it, therefore we can't get the rule
                return NotFound();
            }

            Rule rule = RulesHelper.GetRules(site, ruleId.Path).Where(r => r.Name.ToString().Equals(ruleId.Name)).FirstOrDefault();

            if (rule == null) {
                return NotFound();
            }

            return RulesHelper.ToJsonModel(rule, site, ruleId.Path, Context.Request.GetFields(), true);
        }

        [HttpPost]
        [Audit]
        [ResourceInfo(Name = Defines.FilteringRuleName)]
        public object Post([FromBody] dynamic model)
        {
            Rule rule = null;
            Site site = null;
            RequestFilteringId reqId = null;
            
            if (model == null) {
                throw new ApiArgumentException("model");
            }
            // Rule must be created for a specific request filtering section
            if (model.request_filtering == null) {
                throw new ApiArgumentException("request_filtering");
            }
            if (!(model.request_filtering is JObject)) {
                throw new ApiArgumentException(String.Empty, "request_filtering");
            }
            string reqUuid = DynamicHelper.Value(model.request_filtering.id);
            if (reqUuid == null) {
                throw new ApiArgumentException("request_filtering.id");
            }

            // Get the feature id
            reqId = new RequestFilteringId(reqUuid);

            // Get site the rule is for if applicable
            site = reqId.SiteId == null ? null : SiteHelper.GetSite(reqId.SiteId.Value);

            string configPath = ManagementUnit.ResolveConfigScope(model);
            RequestFilteringSection section = RequestFilteringHelper.GetRequestFilteringSection(site, reqId.Path, configPath);

            // Create filtering rule
            rule = RulesHelper.CreateRule(model, section);

            // Add it
            RulesHelper.AddRule(rule, section);

            // Save
            ManagementUnit.Current.Commit();

            //
            // Create response
            dynamic r = RulesHelper.ToJsonModel(rule, site, reqId.Path, null, true);
            return Created(RulesHelper.GetLocation(r.id), r);
        }

        [HttpPatch]
        [Audit]
        [ResourceInfo(Name = Defines.FilteringRuleName)]
        public object Patch(string id, [FromBody] dynamic model)
        {
            RuleId ruleId = new RuleId(id);

            Site site = ruleId.SiteId == null ? null : SiteHelper.GetSite(ruleId.SiteId.Value);

            if (ruleId.SiteId != null && site == null) {
                // The rule id specified a site but we couldn't find it, therefore we can't get the rule
                return NotFound();
            }

            if(model == null) {
                throw new ApiArgumentException("model");
            }
            
            string configPath = ManagementUnit.ResolveConfigScope(model);
            Rule rule = RulesHelper.GetRules(site, ruleId.Path, configPath).Where(r => r.Name.ToString().Equals(ruleId.Name)).FirstOrDefault();

            if (rule == null) {
                return NotFound();
            }

            rule = RulesHelper.UpdateRule(rule, model);

            ManagementUnit.Current.Commit();

            dynamic rle = RulesHelper.ToJsonModel(rule, site, ruleId.Path, null, true);

            if(rle.id != id) {
                return LocationChanged(RulesHelper.GetLocation(rle.id), rle);
            }

            return rle;
        }

        [HttpDelete]
        [Audit]
        public void Delete(string id)
        {
            RuleId ruleId = new RuleId(id);

            Site site = ruleId.SiteId == null ? null : SiteHelper.GetSite(ruleId.SiteId.Value);
            Application app = ApplicationHelper.GetApplication(ruleId.Path, site);

            if (ruleId.SiteId != null && site == null) {
                Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                return;
            }

            Rule rule = RulesHelper.GetRules(site, ruleId.Path).Where(r => r.Name.ToString().Equals(ruleId.Name)).FirstOrDefault();

            if (rule != null) {

                var section = RequestFilteringHelper.GetRequestFilteringSection(site, ruleId.Path, ManagementUnit.ResolveConfigScope());

                RulesHelper.DeleteRule(rule, section);
                ManagementUnit.Current.Commit();
            }

            Context.Response.StatusCode = (int)HttpStatusCode.NoContent;
        }
    }
}
